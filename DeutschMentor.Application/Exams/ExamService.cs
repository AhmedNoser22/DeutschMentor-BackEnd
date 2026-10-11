namespace DeutschMentor.Application.Exams;

public sealed class ExamService(IApplicationDbContext db, ILearningService learning, TimeProvider clock) : IExamService
{
    private sealed record GradedQuestion(ExamQuestion Question, bool Correct);

    public async Task<ExamOverviewDto> GetOverviewAsync(Guid userId, CancellationToken ct)
    {
        var now = Now();
        await FinalizeExpiredAsync(userId, now, ct);

        var levels = await db.Levels.AsNoTracking().OrderBy(l => l.Order).ToListAsync(ct);
        var summaries = (await learning.GetLevelsAsync(userId, ct)).ToDictionary(s => s.Code);
        var attempts = await db.ExamAttempts.AsNoTracking().Where(a => a.UserId == userId).ToListAsync(ct);
        var passes = await db.LevelPasses.AsNoTracking().Where(p => p.UserId == userId).ToListAsync(ct);
        var certificates = await db.Certificates.AsNoTracking().Where(c => c.UserId == userId).ToListAsync(ct);
        var pools = await PoolSizesAsync(ct);
        var user = await db.Users.AsNoTracking().FirstAsync(u => u.Id == userId, ct);

        var rows = new List<ExamLevelDto>();
        foreach (var level in levels)
        {
            var summary = summaries[level.Code.ToString()];
            var pass = passes.FirstOrDefault(p => p.LevelId == level.Id);
            var certificate = certificates.FirstOrDefault(c => c.LevelId == level.Id);
            var previous = levels.FirstOrDefault(l => l.Order == level.Order - 1);
            var remaining = Math.Max(0, summary.TotalLessons - summary.CompletedLessons);

            var levelAttempts = attempts.Where(a => a.Kind == ExamKind.Level && a.LevelId == level.Id).ToList();
            var running = levelAttempts.FirstOrDefault(a => a.Status == ExamAttemptStatus.InProgress);
            var lastEnded = levelAttempts
                .Where(a => a.Status != ExamAttemptStatus.InProgress && a.SubmittedAtUtc != null)
                .OrderByDescending(a => a.SubmittedAtUtc)
                .FirstOrDefault();
            DateTime? retakeAt = lastEnded?.SubmittedAtUtc + ExamRules.RetakeCooldown;

            ExamAvailability availability;
            if (pass is not null)
            {
                availability = ExamAvailability.Passed;
            }
            else if (summary.Status == LevelStatus.Locked)
            {
                availability = ExamAvailability.LevelLocked;
            }
            else if (running is not null)
            {
                availability = ExamAvailability.InProgress;
            }
            else if (summary.TotalLessons == 0 || remaining > 0)
            {
                availability = ExamAvailability.LessonsRemaining;
            }
            else if (!pools.ContainsKey(level.Id))
            {
                availability = ExamAvailability.NotPublished;
            }
            else if (retakeAt is { } at && at > now)
            {
                availability = ExamAvailability.Cooldown;
            }
            else
            {
                availability = ExamAvailability.Ready;
            }

            rows.Add(new ExamLevelDto(
                level.Code.ToString(),
                level.Title,
                availability,
                remaining,
                previous?.Code.ToString(),
                level.PassMarkPercent,
                pass?.ScorePercent,
                pass?.PassedAtUtc.AsUtc(),
                availability == ExamAvailability.Cooldown ? retakeAt.AsUtc() : null,
                running?.Id,
                certificate?.Id,
                pass is { Source: LevelPassSource.Placement }));
        }

        var placementRunning = attempts.FirstOrDefault(a => a.Kind == ExamKind.Placement && a.Status == ExamAttemptStatus.InProgress);
        var placementAvailable = user.StartPreference == StartPreference.PlacementTest
            && user.PlacementTakenAtUtc is null
            && passes.Count == 0
            && pools.Count > 0;
        var placementCount = levels.Sum(l => pools.TryGetValue(l.Id, out var size) ? Math.Min(size, ExamRules.PlacementPerLevel) : 0);

        var placement = new PlacementInfoDto(
            placementAvailable || placementRunning is not null,
            placementRunning?.Id,
            placementCount,
            ExamRules.PlacementMinutes(placementCount));

        return new ExamOverviewDto(
            rows,
            placement,
            levels.FirstOrDefault()?.PassMarkPercent ?? ExamRules.PlacementPassPercent,
            (int)ExamRules.RetakeCooldown.TotalHours,
            now);
    }

    public async Task<ExamSessionDto> StartLevelExamAsync(Guid userId, string levelCode, CancellationToken ct)
    {
        if (!Enum.TryParse<CefrLevel>(levelCode, true, out var cefr) || !Enum.IsDefined(cefr))
        {
            throw new NotFoundException("level_not_found", "Level not found.");
        }

        var level = await db.Levels.AsNoTracking().FirstOrDefaultAsync(l => l.Code == cefr, ct)
            ?? throw new NotFoundException("level_not_found", "Level not found.");

        var now = Now();
        await FinalizeExpiredAsync(userId, now, ct);

        var running = await db.ExamAttempts.AsNoTracking().FirstOrDefaultAsync(
            a => a.UserId == userId && a.Kind == ExamKind.Level && a.LevelId == level.Id && a.Status == ExamAttemptStatus.InProgress,
            ct);
        if (running is not null)
        {
            return await BuildSessionAsync(running, now, ct);
        }

        var summary = (await learning.GetLevelsAsync(userId, ct)).First(s => s.Code == level.Code.ToString());

        if (summary.Status == LevelStatus.Passed)
        {
            throw new ConflictException("exam_already_passed", "You already passed this level exam.");
        }

        if (summary.Status == LevelStatus.Locked)
        {
            throw new ForbiddenException("level_locked", "Pass the previous level exam first.");
        }

        if (summary.TotalLessons == 0 || summary.CompletedLessons < summary.TotalLessons)
        {
            throw new ForbiddenException("lessons_incomplete", "Finish every lesson of this level before the exam.");
        }

        var lastEnded = await db.ExamAttempts.AsNoTracking()
            .Where(a => a.UserId == userId && a.Kind == ExamKind.Level && a.LevelId == level.Id && a.Status != ExamAttemptStatus.InProgress)
            .OrderByDescending(a => a.SubmittedAtUtc)
            .Select(a => a.SubmittedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (lastEnded is { } ended && ended + ExamRules.RetakeCooldown > now)
        {
            var wait = ended + ExamRules.RetakeCooldown - now;
            throw new TooManyRequestsException("exam_cooldown", $"You can retake this exam in {(int)wait.TotalHours}h {wait.Minutes}m.");
        }

        var pool = await db.ExamQuestions.AsNoTracking()
            .Where(q => q.LevelId == level.Id && q.IsActive)
            .ToListAsync(ct);

        var selected = ExamBuilder.SelectLevelQuestions(pool, ExamRules.LevelQuestionCount);
        if (selected.Count == 0)
        {
            throw new ConflictException("exam_not_published", "The exam for this level is not published yet.");
        }

        return await CreateAttemptAsync(userId, ExamKind.Level, level.Id, selected, ExamRules.LevelMinutes(selected.Count), now, ct);
    }

    public async Task<ExamSessionDto> StartPlacementAsync(Guid userId, CancellationToken ct)
    {
        var now = Now();
        await FinalizeExpiredAsync(userId, now, ct);

        var running = await db.ExamAttempts.AsNoTracking().FirstOrDefaultAsync(
            a => a.UserId == userId && a.Kind == ExamKind.Placement && a.Status == ExamAttemptStatus.InProgress,
            ct);
        if (running is not null)
        {
            return await BuildSessionAsync(running, now, ct);
        }

        var user = await db.Users.AsNoTracking().FirstAsync(u => u.Id == userId, ct);
        var hasPasses = await db.LevelPasses.AnyAsync(p => p.UserId == userId, ct);

        if (user.StartPreference != StartPreference.PlacementTest || user.PlacementTakenAtUtc is not null || hasPasses)
        {
            throw new ConflictException("placement_unavailable", "The placement test is offered once, when you create your account.");
        }

        var levels = await db.Levels.AsNoTracking().OrderBy(l => l.Order).ToListAsync(ct);
        var pool = await db.ExamQuestions.AsNoTracking().Where(q => q.IsActive).ToListAsync(ct);
        var selected = ExamBuilder.SelectPlacementQuestions(levels, pool, ExamRules.PlacementPerLevel);

        if (selected.Count == 0)
        {
            throw new ConflictException("exam_not_published", "The placement test is not published yet.");
        }

        return await CreateAttemptAsync(userId, ExamKind.Placement, null, selected, ExamRules.PlacementMinutes(selected.Count), now, ct);
    }

    public async Task SkipPlacementAsync(Guid userId, CancellationToken ct)
    {
        var user = await db.Users.FirstAsync(u => u.Id == userId, ct);

        if (user.StartPreference != StartPreference.PlacementTest || user.PlacementTakenAtUtc is not null)
        {
            throw new ConflictException("placement_unavailable", "The placement test is offered once, when you create your account.");
        }

        var started = await db.ExamAttempts.AnyAsync(
            a => a.UserId == userId && a.Kind == ExamKind.Placement && a.Status == ExamAttemptStatus.InProgress,
            ct);
        if (started)
        {
            throw new ConflictException("placement_in_progress", "Finish or submit the placement test you already started.");
        }

        user.PlacementTakenAtUtc = Now();
        await db.SaveChangesAsync(ct);
    }

    public async Task<ExamSessionDto> GetSessionAsync(Guid userId, Guid attemptId, CancellationToken ct)
    {
        var attempt = await LoadAttemptAsync(userId, attemptId, ct);
        var now = Now();

        if (attempt.Status == ExamAttemptStatus.InProgress && now > attempt.ExpiresAtUtc + ExamRules.SubmitGrace)
        {
            await GradeFromSavedAsync(attempt, now, ct);
        }

        return await BuildSessionAsync(attempt, now, ct);
    }

    public async Task SaveAnswerAsync(Guid userId, Guid attemptId, Guid questionId, SaveAnswerRequest request, CancellationToken ct)
    {
        var attempt = await LoadAttemptAsync(userId, attemptId, ct);
        var now = Now();

        if (attempt.Status != ExamAttemptStatus.InProgress || now > attempt.ExpiresAtUtc + ExamRules.SubmitGrace)
        {
            throw new ConflictException("exam_closed", "This exam is closed.");
        }

        EnsureQuestionInAttempt(attempt, questionId);
        var answer = NormalizeAnswer(request.Answer);

        var row = await db.ExamAnswers.FirstOrDefaultAsync(a => a.AttemptId == attemptId && a.QuestionId == questionId, ct);
        if (row is null)
        {
            row = new ExamAnswer { AttemptId = attemptId, QuestionId = questionId };
            db.ExamAnswers.Add(row);
        }

        row.AnswerJson = JsonSerializer.Serialize(answer);
        row.IsFlagged = request.Flagged;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            throw new ConflictException("save_conflict", "Your answer could not be saved. Try again.");
        }
    }

    public async Task<ExamResultDto> SubmitAsync(Guid userId, Guid attemptId, SubmitExamRequest request, CancellationToken ct)
    {
        var attempt = await LoadAttemptAsync(userId, attemptId, ct);
        var now = Now();

        if (attempt.Status == ExamAttemptStatus.InProgress)
        {
            var expired = now > attempt.ExpiresAtUtc;
            var acceptsAnswers = !expired || now <= attempt.ExpiresAtUtc + ExamRules.SubmitGrace;

            var answers = await db.ExamAnswers.Where(a => a.AttemptId == attemptId).ToListAsync(ct);
            if (acceptsAnswers && request.Answers is { Count: > 0 } incoming)
            {
                MergeAnswers(attempt, answers, incoming);
            }

            await GradeAsync(attempt, answers, expired ? ExamAttemptStatus.Expired : ExamAttemptStatus.Submitted, now, ct);
        }

        return await BuildResultAsync(attempt, now, ct);
    }

    public async Task<ExamResultDto> GetResultAsync(Guid userId, Guid attemptId, CancellationToken ct)
    {
        var attempt = await LoadAttemptAsync(userId, attemptId, ct);
        var now = Now();

        if (attempt.Status == ExamAttemptStatus.InProgress && now > attempt.ExpiresAtUtc + ExamRules.SubmitGrace)
        {
            await GradeFromSavedAsync(attempt, now, ct);
        }

        if (attempt.Status == ExamAttemptStatus.InProgress)
        {
            throw new ConflictException("exam_not_finished", "This exam is still in progress.");
        }

        return await BuildResultAsync(attempt, now, ct);
    }

    private DateTime Now() => clock.GetUtcNow().UtcDateTime;

    private async Task<ExamAttempt> LoadAttemptAsync(Guid userId, Guid attemptId, CancellationToken ct) =>
        await db.ExamAttempts.FirstOrDefaultAsync(a => a.Id == attemptId && a.UserId == userId, ct)
        ?? throw new NotFoundException("attempt_not_found", "Exam attempt not found.");

    private async Task<Dictionary<Guid, int>> PoolSizesAsync(CancellationToken ct) =>
        await db.ExamQuestions.AsNoTracking()
            .Where(q => q.IsActive)
            .GroupBy(q => q.LevelId)
            .Select(g => new { LevelId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.LevelId, x => x.Count, ct);

    private async Task<ExamSessionDto> CreateAttemptAsync(
        Guid userId,
        ExamKind kind,
        Guid? levelId,
        List<ExamQuestion> selected,
        int minutes,
        DateTime now,
        CancellationToken ct)
    {
        var attempt = new ExamAttempt
        {
            UserId = userId,
            Kind = kind,
            LevelId = levelId,
            Status = ExamAttemptStatus.InProgress,
            StartedAtUtc = now,
            ExpiresAtUtc = now.AddMinutes(minutes),
            QuestionIdsJson = JsonSerializer.Serialize(selected.Select(q => q.Id).ToList()),
            TotalQuestions = selected.Count
        };

        db.ExamAttempts.Add(attempt);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            var existing = await db.ExamAttempts.AsNoTracking().FirstOrDefaultAsync(
                a => a.UserId == userId && a.Kind == kind && a.LevelId == levelId
                    && a.Status == ExamAttemptStatus.InProgress && a.Id != attempt.Id,
                ct);

            if (existing is null)
            {
                throw;
            }

            return await BuildSessionAsync(existing, now, ct);
        }

        return await BuildSessionAsync(attempt, now, ct);
    }

    private async Task<ExamSessionDto> BuildSessionAsync(ExamAttempt attempt, DateTime now, CancellationToken ct)
    {
        var level = attempt.LevelId is null
            ? null
            : await db.Levels.AsNoTracking().FirstOrDefaultAsync(l => l.Id == attempt.LevelId, ct);

        var levelCode = level?.Code.ToString();
        var title = TitleOf(attempt, level);

        if (attempt.Status != ExamAttemptStatus.InProgress)
        {
            return new ExamSessionDto(
                attempt.Id, attempt.Kind, levelCode, title, attempt.Status,
                attempt.StartedAtUtc.AsUtc(), attempt.ExpiresAtUtc.AsUtc(), now, [], []);
        }

        var ids = ParseIds(attempt.QuestionIdsJson);
        var questions = (await db.ExamQuestions.AsNoTracking().Where(q => ids.Contains(q.Id)).ToListAsync(ct))
            .ToDictionary(q => q.Id);

        var dtos = new List<ExamQuestionDto>();
        for (var i = 0; i < ids.Count; i++)
        {
            if (questions.TryGetValue(ids[i], out var question))
            {
                var exercise = ExerciseGrader.ToDto(question.Id, i + 1, question.Type, question.Prompt, question.PayloadJson);
                dtos.Add(new ExamQuestionDto(exercise, question.Skill));
            }
        }

        var saved = (await db.ExamAnswers.AsNoTracking().Where(a => a.AttemptId == attempt.Id).ToListAsync(ct))
            .Select(a => new SavedAnswerDto(a.QuestionId, ParseAnswer(a.AnswerJson), a.IsFlagged))
            .ToList();

        return new ExamSessionDto(
            attempt.Id, attempt.Kind, levelCode, title, attempt.Status,
            attempt.StartedAtUtc.AsUtc(), attempt.ExpiresAtUtc.AsUtc(), now, dtos, saved);
    }

    private async Task FinalizeExpiredAsync(Guid userId, DateTime now, CancellationToken ct)
    {
        var cutoff = now - ExamRules.SubmitGrace;
        var expired = await db.ExamAttempts
            .Where(a => a.UserId == userId && a.Status == ExamAttemptStatus.InProgress && a.ExpiresAtUtc < cutoff)
            .ToListAsync(ct);

        foreach (var attempt in expired)
        {
            await GradeFromSavedAsync(attempt, now, ct);
        }
    }

    private async Task GradeFromSavedAsync(ExamAttempt attempt, DateTime now, CancellationToken ct)
    {
        var answers = await db.ExamAnswers.Where(a => a.AttemptId == attempt.Id).ToListAsync(ct);
        await GradeAsync(attempt, answers, ExamAttemptStatus.Expired, now, ct);
    }

    private void MergeAnswers(ExamAttempt attempt, List<ExamAnswer> existing, List<ExamAnswerInput> incoming)
    {
        if (incoming.Count > 200)
        {
            throw new BadRequestException("invalid_answer", "Too many answers.");
        }

        var allowed = ParseIds(attempt.QuestionIdsJson).ToHashSet();

        foreach (var item in incoming)
        {
            if (!allowed.Contains(item.QuestionId))
            {
                throw new BadRequestException("invalid_question", "This question is not part of the exam.");
            }

            var answer = NormalizeAnswer(item.Answer);
            var row = existing.FirstOrDefault(a => a.QuestionId == item.QuestionId);

            if (row is null)
            {
                row = new ExamAnswer { AttemptId = attempt.Id, QuestionId = item.QuestionId };
                db.ExamAnswers.Add(row);
                existing.Add(row);
            }

            row.AnswerJson = JsonSerializer.Serialize(answer);
            row.IsFlagged = item.Flagged;
        }
    }

    private async Task GradeAsync(
        ExamAttempt attempt,
        List<ExamAnswer> answers,
        ExamAttemptStatus status,
        DateTime now,
        CancellationToken ct)
    {
        var ids = ParseIds(attempt.QuestionIdsJson);
        var questions = await db.ExamQuestions.AsNoTracking().Where(q => ids.Contains(q.Id)).ToListAsync(ct);
        var byQuestion = answers.ToDictionary(a => a.QuestionId);

        var graded = new List<GradedQuestion>();
        foreach (var question in questions)
        {
            var correct = false;

            if (byQuestion.TryGetValue(question.Id, out var saved))
            {
                var given = ParseAnswer(saved.AnswerJson);
                correct = given.Count > 0 && IsCorrect(question, given);
                saved.IsCorrect = correct;
            }

            graded.Add(new GradedQuestion(question, correct));
        }

        var correctCount = graded.Count(g => g.Correct);
        var endedAt = status == ExamAttemptStatus.Expired ? attempt.ExpiresAtUtc : now;

        attempt.Status = status;
        attempt.SubmittedAtUtc = endedAt;
        attempt.TotalQuestions = graded.Count;
        attempt.CorrectCount = correctCount;
        attempt.ScorePercent = Percent(correctCount, graded.Count);

        if (attempt.Kind == ExamKind.Level)
        {
            await ApplyLevelResultAsync(attempt, graded, endedAt, ct);
        }
        else
        {
            await ApplyPlacementResultAsync(attempt, graded, endedAt, ct);
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            throw new ConflictException("exam_conflict", "This exam was already submitted. Open it again to see the result.");
        }
    }

    private async Task ApplyLevelResultAsync(ExamAttempt attempt, List<GradedQuestion> graded, DateTime at, CancellationToken ct)
    {
        var level = await db.Levels.AsNoTracking().FirstAsync(l => l.Id == attempt.LevelId, ct);

        var breakdown = new List<ScoreBreakdownDto>();
        foreach (var skill in ExamBuilder.SkillOrder)
        {
            var items = graded.Where(g => g.Question.Skill == skill).ToList();
            if (items.Count > 0)
            {
                var correct = items.Count(i => i.Correct);
                breakdown.Add(new ScoreBreakdownDto(skill.ToString(), correct, items.Count, Percent(correct, items.Count)));
            }
        }

        attempt.SkillScoresJson = JsonSerializer.Serialize(breakdown, JsonSerializerOptions.Web);
        attempt.Passed = attempt.ScorePercent >= level.PassMarkPercent;

        if (!attempt.Passed)
        {
            return;
        }

        var alreadyPassed = await db.LevelPasses.AnyAsync(p => p.UserId == attempt.UserId && p.LevelId == level.Id, ct);
        if (alreadyPassed)
        {
            return;
        }

        var user = await db.Users.FirstAsync(u => u.Id == attempt.UserId, ct);

        db.LevelPasses.Add(new LevelPass
        {
            UserId = user.Id,
            LevelId = level.Id,
            ScorePercent = attempt.ScorePercent,
            PassedAtUtc = at,
            Source = LevelPassSource.Exam
        });

        db.Certificates.Add(CertificateIssuer.Create(user, level, attempt.Id, attempt.ScorePercent, at));

        var next = await db.Levels.AsNoTracking().FirstOrDefaultAsync(l => l.Order == level.Order + 1, ct);
        if (next is not null && next.Code > user.CurrentLevel)
        {
            user.CurrentLevel = next.Code;
        }
    }

    private async Task ApplyPlacementResultAsync(ExamAttempt attempt, List<GradedQuestion> graded, DateTime at, CancellationToken ct)
    {
        var levels = await db.Levels.AsNoTracking().OrderBy(l => l.Order).ToListAsync(ct);

        var perLevel = levels
            .Select(level =>
            {
                var items = graded.Where(g => g.Question.LevelId == level.Id).ToList();
                return (Level: level, Correct: items.Count(i => i.Correct), Total: items.Count);
            })
            .ToList();

        var breakdown = perLevel
            .Where(x => x.Total > 0)
            .Select(x => new ScoreBreakdownDto(x.Level.Code.ToString(), x.Correct, x.Total, Percent(x.Correct, x.Total)))
            .ToList();

        attempt.SkillScoresJson = JsonSerializer.Serialize(breakdown, JsonSerializerOptions.Web);

        var start = levels[0];
        var granted = new List<(Level Level, int Score)>();

        for (var i = 0; i < perLevel.Count; i++)
        {
            var row = perLevel[i];
            var isTopLevel = i == perLevel.Count - 1;
            var percent = Percent(row.Correct, row.Total);
            var mastered = !isTopLevel && row.Total > 0 && percent >= row.Level.PassMarkPercent;

            if (!mastered)
            {
                start = row.Level;
                break;
            }

            granted.Add((row.Level, percent));
        }

        var user = await db.Users.FirstAsync(u => u.Id == attempt.UserId, ct);
        var existing = await db.LevelPasses.Where(p => p.UserId == user.Id).Select(p => p.LevelId).ToListAsync(ct);

        foreach (var (level, score) in granted)
        {
            if (!existing.Contains(level.Id))
            {
                db.LevelPasses.Add(new LevelPass
                {
                    UserId = user.Id,
                    LevelId = level.Id,
                    ScorePercent = score,
                    PassedAtUtc = at,
                    Source = LevelPassSource.Placement
                });
            }
        }

        user.CurrentLevel = start.Code;
        user.PlacementTakenAtUtc = at;
        attempt.PlacedLevelId = start.Id;
        attempt.Passed = true;
    }

    private async Task<ExamResultDto> BuildResultAsync(ExamAttempt attempt, DateTime now, CancellationToken ct)
    {
        var level = attempt.LevelId is null
            ? null
            : await db.Levels.AsNoTracking().FirstOrDefaultAsync(l => l.Id == attempt.LevelId, ct);

        var breakdown = ParseBreakdown(attempt.SkillScoresJson);
        var passMark = level?.PassMarkPercent ?? ExamRules.PlacementPassPercent;
        var weakAreas = attempt.Kind == ExamKind.Level
            ? breakdown.Where(b => b.Percent < passMark).Select(b => b.Label).ToList()
            : [];

        Guid? certificateId = null;
        string? nextLevelCode = null;
        string? placedLevelCode = null;
        DateTime? retakeAt = null;

        if (attempt.Kind == ExamKind.Level && level is not null)
        {
            if (attempt.Passed)
            {
                certificateId = await db.Certificates.AsNoTracking()
                    .Where(c => c.ExamAttemptId == attempt.Id)
                    .Select(c => (Guid?)c.Id)
                    .FirstOrDefaultAsync(ct);

                var next = await db.Levels.AsNoTracking().FirstOrDefaultAsync(l => l.Order == level.Order + 1, ct);
                nextLevelCode = next?.Code.ToString();
            }
            else if (attempt.SubmittedAtUtc is { } submitted)
            {
                var at = submitted + ExamRules.RetakeCooldown;
                if (at > now)
                {
                    retakeAt = at;
                }
            }
        }

        if (attempt.Kind == ExamKind.Placement && attempt.PlacedLevelId is { } placedId)
        {
            var placed = await db.Levels.AsNoTracking().FirstAsync(l => l.Id == placedId, ct);
            placedLevelCode = placed.Code.ToString();
        }

        return new ExamResultDto(
            attempt.Id,
            attempt.Kind,
            level?.Code.ToString(),
            TitleOf(attempt, level),
            attempt.Status,
            attempt.ScorePercent,
            attempt.CorrectCount,
            attempt.TotalQuestions,
            passMark,
            attempt.Passed,
            breakdown,
            weakAreas,
            (attempt.SubmittedAtUtc ?? now).AsUtc(),
            retakeAt.AsUtc(),
            now,
            certificateId,
            nextLevelCode,
            placedLevelCode);
    }

    private static string TitleOf(ExamAttempt attempt, Level? level) =>
        attempt.Kind == ExamKind.Placement || level is null ? "Placement test" : $"{level.Code} level exam";

    private static bool IsCorrect(ExamQuestion question, IReadOnlyList<string> given)
    {
        try
        {
            return ExerciseGrader.Grade(question.Type, question.PayloadJson, given).IsCorrect;
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException or JsonException)
        {
            return false;
        }
    }

    private static void EnsureQuestionInAttempt(ExamAttempt attempt, Guid questionId)
    {
        if (!ParseIds(attempt.QuestionIdsJson).Contains(questionId))
        {
            throw new BadRequestException("invalid_question", "This question is not part of the exam.");
        }
    }

    private static List<string> NormalizeAnswer(List<string>? answer)
    {
        if (answer is null)
        {
            return [];
        }

        if (answer.Count > 30 || answer.Any(a => a is null || a.Length > 200))
        {
            throw new BadRequestException("invalid_answer", "The answer is not valid.");
        }

        return answer;
    }

    private static int Percent(int correct, int total) =>
        total == 0 ? 0 : (int)Math.Round(100.0 * correct / total, MidpointRounding.AwayFromZero);

    private static List<Guid> ParseIds(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<Guid>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static List<string> ParseAnswer(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static List<ScoreBreakdownDto> ParseBreakdown(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<ScoreBreakdownDto>>(json, JsonSerializerOptions.Web) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}