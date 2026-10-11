namespace DeutschMentor.Application.Progress;

public sealed class ProgressService(
    IApplicationDbContext db,
    ILearningService learning,
    IAchievementService achievements,
    TimeProvider clock) : IProgressService
{
    private const int LessonSteps = 5;
    private const int WarmUpWords = 5;
    private const int WarmUpMinutes = 4;

    public async Task<TodayDto> GetTodayAsync(Guid userId, int offsetMinutes, CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var offset = LocalDay.ClampOffset(offsetMinutes);
        var today = LocalDay.Today(now, offset);
        var dayStart = LocalDay.StartUtc(now, offset);
        var dayEnd = dayStart.AddDays(1);

        var days = await ActivityReader.GetDaysAsync(db, userId, now, offset, ct);
        var streak = StreakCalculator.Calculate(days, today);
        var week = BuildWeek(days, today);
        var current = await FindContinueAsync(userId, ct);

        var due = await db.ReviewItems.CountAsync(r => r.UserId == userId && r.DueAtUtc < dayEnd, ct);
        var reviewedToday = await db.ReviewLogs.CountAsync(
            l => l.UserId == userId && l.CreatedAtUtc >= dayStart && l.CreatedAtUtc < dayEnd, ct);
        var addedToday = await db.ReviewItems.CountAsync(
            r => r.UserId == userId && r.CreatedAtUtc >= dayStart && r.CreatedAtUtc < dayEnd, ct);
        var lessonsToday = await db.LessonProgress.CountAsync(
            p => p.UserId == userId && p.CompletedAtUtc >= dayStart && p.CompletedAtUtc < dayEnd, ct);
        var drillToday = await db.DrillAttempts.CountAsync(
            a => a.UserId == userId && a.CreatedAtUtc >= dayStart && a.CreatedAtUtc < dayEnd, ct);

        var newWords = 0;
        if (current is not null)
        {
            newWords = await db.VocabularyItems.CountAsync(
                v => v.LessonId == current.LessonId
                    && !db.ReviewItems.Any(r => r.UserId == userId && r.VocabularyItemId == v.Id),
                ct);
        }

        var nouns = await CountNounsAsync(userId, ct);
        var plan = BuildPlan(current, due, reviewedToday, addedToday, lessonsToday, drillToday, newWords, nouns);

        var mentor = MentorAdvisor.Build(await LoadMentorFactsAsync(userId, now, today, streak, due, nouns, ct));

        var recent = (await achievements.EvaluateAsync(userId, streak, ct))
            .Where(a => a.Unlocked)
            .OrderByDescending(a => a.UnlockedAtUtc)
            .Take(3)
            .ToList();

        return new TodayDto(
            streak,
            week,
            current,
            plan,
            new ReviewSummaryDto(due, ReviewScheduler.EstimateMinutes(due)),
            mentor,
            recent,
            now);
    }

    private async Task<ContinueLessonDto?> FindContinueAsync(Guid userId, CancellationToken ct)
    {
        var levels = await learning.GetLevelsAsync(userId, ct);
        var active = levels.FirstOrDefault(l => l.Status == LevelStatus.InProgress);
        if (active is null)
        {
            return null;
        }

        var detail = await learning.GetLevelAsync(userId, active.Code, ct);

        foreach (var module in detail.Modules)
        {
            foreach (var unit in module.Units)
            {
                var lesson = unit.Lessons.FirstOrDefault(l => l.State == LessonState.Current);
                if (lesson is null)
                {
                    continue;
                }

                var full = await learning.GetLessonAsync(userId, lesson.Id, ct);
                var done = full.StepsSeen.Count;
                var left = (int)Math.Ceiling(lesson.EstimatedMinutes * (LessonSteps - done) / (double)LessonSteps);

                return new ContinueLessonDto(
                    lesson.Id,
                    lesson.Code,
                    active.Code,
                    module.Order,
                    unit.Order,
                    lesson.Title,
                    lesson.TitleEn,
                    full.TitleAr,
                    done,
                    LessonSteps,
                    Math.Max(1, left));
            }
        }

        return null;
    }

    private async Task<int> CountNounsAsync(Guid userId, CancellationToken ct)
    {
        var lessonIds = (await learning.GetAccessibleLessonIdsAsync(userId, ct)).ToList();
        return await db.VocabularyItems.CountAsync(v => lessonIds.Contains(v.LessonId) && v.Article != null, ct);
    }

    private static DailyPlanDto BuildPlan(
        ContinueLessonDto? current,
        int due,
        int reviewedToday,
        int addedToday,
        int lessonsToday,
        int drillToday,
        int newWords,
        int nouns)
    {
        var items = new List<PlanItemDto>();

        if (newWords > 0 || addedToday > 0)
        {
            var count = addedToday > 0 ? addedToday : Math.Min(WarmUpWords, newWords);
            items.Add(new PlanItemDto(
                PlanItemKind.WarmUp,
                $"Warm-up: {count} new {(count == 1 ? "word" : "words")}",
                WarmUpMinutes,
                addedToday > 0,
                current?.LessonId));
        }

        if (due > 0 || reviewedToday > 0)
        {
            var count = due > 0 ? due : reviewedToday;
            var noun = count == 1 ? "card" : "cards";
            items.Add(new PlanItemDto(
                PlanItemKind.Review,
                due > 0 ? $"Review {due} due {noun}" : $"Review {reviewedToday} {noun}",
                ReviewScheduler.EstimateMinutes(count),
                due == 0,
                null));
        }

        if (current is not null || lessonsToday > 0)
        {
            items.Add(new PlanItemDto(
                PlanItemKind.Lesson,
                lessonsToday > 0 ? "Lesson finished today" : current!.Title,
                lessonsToday > 0 ? 0 : current!.MinutesLeft,
                lessonsToday > 0,
                current?.LessonId));
        }

        if (nouns >= DrillRules.MinWords)
        {
            items.Add(new PlanItemDto(
                PlanItemKind.Drill,
                "Article drill: der, die, das",
                DrillRules.Minutes,
                drillToday >= Math.Min(DrillRules.Size, nouns),
                null));
        }

        return new DailyPlanDto(
            items,
            items.Count(i => i.Done),
            items.Where(i => !i.Done).Sum(i => i.Minutes));
    }

    private async Task<MentorFacts> LoadMentorFactsAsync(
        Guid userId,
        DateTime now,
        DateOnly today,
        StreakDto streak,
        int due,
        int nouns,
        CancellationToken ct)
    {
        var since7 = now.AddDays(-7);
        var since14 = now.AddDays(-14);

        var attempts = await db.ExerciseAttempts.CountAsync(a => a.UserId == userId && a.CreatedAtUtc >= since7, ct);
        var correct = await db.ExerciseAttempts.CountAsync(
            a => a.UserId == userId && a.CreatedAtUtc >= since7 && a.IsCorrect, ct);
        var misses = await db.DrillAttempts.CountAsync(
            a => a.UserId == userId && !a.IsCorrect && a.CreatedAtUtc >= since7, ct);

        var failed = await (
                from a in db.ExamAttempts.AsNoTracking()
                join l in db.Levels.AsNoTracking() on a.LevelId equals (Guid?)l.Id
                where a.UserId == userId
                    && a.Kind == ExamKind.Level
                    && a.Status != ExamAttemptStatus.InProgress
                    && !a.Passed
                    && a.SubmittedAtUtc >= since14
                orderby a.SubmittedAtUtc descending
                select new { l.Code, l.PassMarkPercent, a.SkillScoresJson })
            .FirstOrDefaultAsync(ct);

        return new MentorFacts(
            due,
            streak.Current,
            streak.StudiedToday,
            attempts,
            correct,
            misses,
            nouns >= DrillRules.MinWords,
            DrillRules.Minutes,
            failed?.Code.ToString(),
            failed is null ? [] : MentorAdvisor.WeakSkills(failed.SkillScoresJson, failed.PassMarkPercent),
            GermanDays.For(today));
    }

    private static List<WeekDayDto> BuildWeek(IReadOnlySet<DateOnly> days, DateOnly today)
    {
        var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));

        return Enumerable.Range(0, 7)
            .Select(i =>
            {
                var date = monday.AddDays(i);
                return new WeekDayDto(
                    date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    days.Contains(date),
                    date == today,
                    date > today);
            })
            .ToList();
    }
}