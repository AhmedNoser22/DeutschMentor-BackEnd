namespace DeutschMentor.Application.Admin;

public interface IAdminService
{
    Task<AdminStatsDto> GetStatsAsync(CancellationToken ct);
    Task<PagedResult<AdminLessonDto>> GetLessonsAsync(string? search, string? level, string? status, int page, CancellationToken ct);
    Task SetStatusAsync(Guid lessonId, ContentStatus status, CancellationToken ct);
    Task DeleteLessonAsync(Guid lessonId, CancellationToken ct);
    Task<IReadOnlyList<AdminWritingDto>> GetPendingWritingAsync(CancellationToken ct);
    Task ReviewWritingAsync(Guid submissionId, ReviewWritingRequest request, Guid adminId, CancellationToken ct);
    Task<IReadOnlyList<ExamPoolDto>> GetExamPoolAsync(CancellationToken ct);
}

public sealed class AdminService(IApplicationDbContext db, TimeProvider clock) : IAdminService
{
    private const int PageSize = 20;

    public async Task<AdminStatsDto> GetStatsAsync(CancellationToken ct) => new(
        await db.Users.CountAsync(u => u.Role == UserRole.Student, ct),
        await db.Lessons.CountAsync(l => l.Status == ContentStatus.Published, ct),
        await db.Lessons.CountAsync(l => l.Status == ContentStatus.Draft, ct),
        await db.ExamAttempts.CountAsync(a => a.Status != ExamAttemptStatus.InProgress, ct));

    public async Task<PagedResult<AdminLessonDto>> GetLessonsAsync(
        string? search, string? level, string? status, int page, CancellationToken ct)
    {
        var query = db.Lessons.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(l => l.Code.Contains(term) || l.Title.Contains(term) || l.TitleEn.Contains(term));
        }

        if (Enum.TryParse<CefrLevel>(level, true, out var cefr) && Enum.IsDefined(cefr))
        {
            query = query.Where(l => l.Unit!.Module!.Level!.Code == cefr);
        }

        if (Enum.TryParse<ContentStatus>(status, true, out var state) && Enum.IsDefined(state))
        {
            query = query.Where(l => l.Status == state);
        }

        var current = Math.Max(1, page);
        var total = await query.CountAsync(ct);

        var rows = await query
            .OrderBy(l => l.Unit!.Module!.Level!.Order)
            .ThenBy(l => l.Unit!.Module!.Order)
            .ThenBy(l => l.Unit!.Order)
            .ThenBy(l => l.Order)
            .Skip((current - 1) * PageSize)
            .Take(PageSize)
            .Select(l => new
            {
                l.Id,
                l.Code,
                l.Title,
                Level = l.Unit!.Module!.Level!.Code,
                l.Status,
                l.CreatedAtUtc,
                Words = db.VocabularyItems.Count(v => v.LessonId == l.Id),
                Grammar = db.GrammarTopics.Count(g => g.LessonId == l.Id),
                Exercises = db.Exercises.Count(e => e.LessonId == l.Id && e.ReadingPassageId == null),
                Reading = db.ReadingPassages.Count(p => p.LessonId == l.Id),
                Writing = db.WritingTasks.Count(t => t.LessonId == l.Id)
            })
            .ToListAsync(ct);

        var items = rows
            .Select(r => new AdminLessonDto(
                r.Id, r.Code, r.Title, r.Level.ToString(), r.Status, r.CreatedAtUtc.AsUtc(),
                r.Words, r.Grammar, r.Exercises, r.Reading, r.Writing))
            .ToList();

        return new PagedResult<AdminLessonDto>(items, total, current, PageSize);
    }

    public async Task SetStatusAsync(Guid lessonId, ContentStatus status, CancellationToken ct)
    {
        var lesson = await db.Lessons.FirstOrDefaultAsync(l => l.Id == lessonId, ct)
            ?? throw new NotFoundException("lesson_not_found", "Lesson not found.");

        if (status == ContentStatus.Published && !await db.VocabularyItems.AnyAsync(v => v.LessonId == lessonId, ct))
        {
            throw new ConflictException("lesson_empty", "A lesson needs vocabulary before it can be published.");
        }

        lesson.Status = status;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteLessonAsync(Guid lessonId, CancellationToken ct)
    {
        var lesson = await db.Lessons.FirstOrDefaultAsync(l => l.Id == lessonId, ct)
            ?? throw new NotFoundException("lesson_not_found", "Lesson not found.");

        if (lesson.Status != ContentStatus.Draft)
        {
            throw new ConflictException("lesson_published", "Unpublish the lesson before deleting it.");
        }

        if (await db.LessonProgress.AnyAsync(p => p.LessonId == lessonId, ct))
        {
            throw new ConflictException("lesson_has_progress", "Students already made progress in this lesson.");
        }

        db.Lessons.Remove(lesson);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<AdminWritingDto>> GetPendingWritingAsync(CancellationToken ct)
    {
        var rows = await (
                from s in db.WritingSubmissions.AsNoTracking()
                join t in db.WritingTasks.AsNoTracking() on s.WritingTaskId equals t.Id
                join u in db.Users.AsNoTracking() on s.UserId equals u.Id
                where s.ReviewStatus == WritingReviewStatus.Pending
                orderby s.CreatedAtUtc
                select new { s.Id, u.FullName, t.Title, t.Prompt, s.Text, s.ScorePercent, t.SampleAnswer, s.CreatedAtUtc })
            .Take(50)
            .ToListAsync(ct);

        return rows
            .Select(r => new AdminWritingDto(r.Id, r.FullName, r.Title, r.Prompt, r.Text, r.ScorePercent, r.SampleAnswer, r.CreatedAtUtc.AsUtc()))
            .ToList();
    }

    public async Task ReviewWritingAsync(Guid submissionId, ReviewWritingRequest request, Guid adminId, CancellationToken ct)
    {
        var submission = await db.WritingSubmissions.FirstOrDefaultAsync(s => s.Id == submissionId, ct)
            ?? throw new NotFoundException("submission_not_found", "Submission not found.");

        var feedback = (request.Feedback ?? string.Empty).Trim();
        if (feedback.Length is 0 or > 1000)
        {
            throw new BadRequestException("invalid_feedback", "Write feedback (up to 1000 characters).");
        }

        if (request.ScorePercent is < 0 or > 100)
        {
            throw new BadRequestException("invalid_score", "The score must be between 0 and 100.");
        }

        submission.TeacherFeedback = feedback;
        submission.TeacherScorePercent = request.ScorePercent;
        submission.ReviewStatus = WritingReviewStatus.Reviewed;
        submission.ReviewedAtUtc = clock.GetUtcNow().UtcDateTime;
        submission.ReviewedByUserId = adminId;
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ExamPoolDto>> GetExamPoolAsync(CancellationToken ct)
    {
        var rows = await (
                from q in db.ExamQuestions.AsNoTracking()
                where q.IsActive
                group q by new { q.Level!.Code, q.Level.Order, q.Skill } into g
                orderby g.Key.Order, g.Key.Skill
                select new { g.Key.Code, g.Key.Skill, Count = g.Count() })
            .ToListAsync(ct);

        return rows.Select(r => new ExamPoolDto(r.Code.ToString(), r.Skill.ToString(), r.Count)).ToList();
    }
}