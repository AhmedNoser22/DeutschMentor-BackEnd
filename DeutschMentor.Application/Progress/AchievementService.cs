namespace DeutschMentor.Application.Progress;

internal sealed record CertificateFact(int ScorePercent, DateTime IssuedAtUtc);

internal sealed record AchievementSnapshot(
    int Lessons,
    int Words,
    int Reviews,
    int LongestStreak,
    bool PerfectExam,
    IReadOnlyList<CefrLevel> WordLevels,
    IReadOnlyDictionary<CefrLevel, CertificateFact> Certificates);

internal sealed record AchievementDefinition(
    string Code,
    string Title,
    string Description,
    string Icon,
    int Target,
    Func<AchievementSnapshot, int> Current);

public sealed class AchievementService(IApplicationDbContext db, TimeProvider clock) : IAchievementService
{
    private static readonly IReadOnlyList<AchievementDefinition> Definitions = BuildDefinitions();

    public async Task<IReadOnlyList<AchievementDto>> GetAllAsync(Guid userId, int offsetMinutes, CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var offset = LocalDay.ClampOffset(offsetMinutes);
        var days = await ActivityReader.GetDaysAsync(db, userId, now, offset, ct);
        var streak = StreakCalculator.Calculate(days, LocalDay.Today(now, offset));
        return await EvaluateAsync(userId, streak, ct);
    }

    public async Task<IReadOnlyList<AchievementDto>> EvaluateAsync(Guid userId, StreakDto streak, CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var snapshot = await LoadSnapshotAsync(userId, streak, ct);

        var stored = await db.UserAchievements.Where(a => a.UserId == userId).ToListAsync(ct);
        var byCode = stored.ToDictionary(a => a.Code);

        foreach (var definition in Definitions)
        {
            if (definition.Current(snapshot) < definition.Target || byCode.ContainsKey(definition.Code))
            {
                continue;
            }

            var unlockedAt = now;
            if (definition.Code.StartsWith("level-", StringComparison.Ordinal)
                && Enum.TryParse<CefrLevel>(definition.Code["level-".Length..], out var level)
                && snapshot.Certificates.TryGetValue(level, out var certificate))
            {
                unlockedAt = certificate.IssuedAtUtc;
            }

            var row = new UserAchievement { UserId = userId, Code = definition.Code, UnlockedAtUtc = unlockedAt };
            db.UserAchievements.Add(row);
            byCode[definition.Code] = row;
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Another request unlocked the same achievement first. The in-memory result is still correct.
        }

        return Definitions
            .Select(definition =>
            {
                var current = Math.Min(definition.Current(snapshot), definition.Target);
                var unlocked = byCode.TryGetValue(definition.Code, out var row);

                return new AchievementDto(
                    definition.Code,
                    definition.Title,
                    definition.Description,
                    definition.Icon,
                    unlocked,
                    unlocked ? row!.UnlockedAtUtc.AsUtc() : null,
                    unlocked ? DetailFor(definition, snapshot, row!.UnlockedAtUtc) : null,
                    current,
                    definition.Target);
            })
            .ToList();
    }

    private async Task<AchievementSnapshot> LoadSnapshotAsync(Guid userId, StreakDto streak, CancellationToken ct)
    {
        var lessons = await db.LessonProgress.CountAsync(p => p.UserId == userId && p.CompletedAtUtc != null, ct);
        var reviews = await db.ReviewLogs.CountAsync(l => l.UserId == userId, ct);

        var wordLevels = await (
                from r in db.ReviewItems.AsNoTracking()
                join v in db.VocabularyItems.AsNoTracking() on r.VocabularyItemId equals v.Id
                where r.UserId == userId && r.Repetitions >= 1
                select v.Lesson!.Unit!.Module!.Level!.Code)
            .ToListAsync(ct);

        var certificates = (await (
                from c in db.Certificates.AsNoTracking()
                join l in db.Levels.AsNoTracking() on c.LevelId equals l.Id
                where c.UserId == userId
                select new { l.Code, c.ScorePercent, c.IssuedAtUtc })
            .ToListAsync(ct))
            .ToDictionary(x => x.Code, x => new CertificateFact(x.ScorePercent, x.IssuedAtUtc));

        var perfect = await db.ExamAttempts.AnyAsync(
            a => a.UserId == userId
                && a.Kind == ExamKind.Level
                && a.Status != ExamAttemptStatus.InProgress
                && a.ScorePercent == 100
                && a.TotalQuestions >= 4,
            ct);

        return new AchievementSnapshot(lessons, wordLevels.Count, reviews, streak.Longest, perfect, wordLevels, certificates);
    }

    private static string? DetailFor(AchievementDefinition definition, AchievementSnapshot snapshot, DateTime unlockedAt)
    {
        if (definition.Code.StartsWith("words-", StringComparison.Ordinal))
        {
            var levels = snapshot.WordLevels.Distinct().Order().Select(l => l.ToString()).ToList();
            return levels.Count == 0 ? null : $"Across {JoinNatural(levels)}";
        }

        if (definition.Code.StartsWith("level-", StringComparison.Ordinal)
            && Enum.TryParse<CefrLevel>(definition.Code["level-".Length..], out var level)
            && snapshot.Certificates.TryGetValue(level, out var certificate))
        {
            return $"Passed with {certificate.ScorePercent}% on {FormatDate(certificate.IssuedAtUtc)}";
        }

        if (definition.Code.StartsWith("streak-", StringComparison.Ordinal))
        {
            return $"Reached on {FormatDate(unlockedAt)}";
        }

        return $"Unlocked on {FormatDate(unlockedAt)}";
    }

    private static string FormatDate(DateTime value) => value.ToString("d MMM", CultureInfo.InvariantCulture);

    private static string JoinNatural(IReadOnlyList<string> items) => items.Count switch
    {
        1 => items[0],
        2 => $"{items[0]} and {items[1]}",
        _ => $"{string.Join(", ", items.Take(items.Count - 1))} and {items[^1]}"
    };

    private static List<AchievementDefinition> BuildDefinitions()
    {
        var list = new List<AchievementDefinition>
        {
            new("first-lesson", "First lesson", "Finish your first lesson.", "book", 1, s => s.Lessons),
            new("lessons-10", "10 lessons", "Finish 10 lessons.", "book", 10, s => s.Lessons),
            new("lessons-25", "25 lessons", "Finish 25 lessons.", "book", 25, s => s.Lessons),
            new("words-50", "50 words learned", "Recall 50 words in your spaced review.", "book", 50, s => s.Words),
            new("words-100", "100 words learned", "Recall 100 words in your spaced review.", "book", 100, s => s.Words),
            new("words-250", "250 words learned", "Recall 250 words in your spaced review.", "book", 250, s => s.Words),
            new("words-500", "500 words learned", "Recall 500 words in your spaced review.", "book", 500, s => s.Words),
            new("reviews-100", "100 reviews", "Rate 100 review cards.", "review", 100, s => s.Reviews),
            new("streak-3", "3-day streak", "Study 3 days in a row.", "flame", 3, s => s.LongestStreak),
            new("streak-7", "7-day streak", "Study 7 days in a row.", "flame", 7, s => s.LongestStreak),
            new("streak-14", "14-day streak", "Study 14 days in a row.", "flame", 14, s => s.LongestStreak),
            new("streak-30", "30-day streak", "Study 30 days in a row.", "flame", 30, s => s.LongestStreak),
            new("perfect-exam", "Perfect score", "Score 100% on a level exam.", "award", 1, s => s.PerfectExam ? 1 : 0)
        };

        foreach (var level in Enum.GetValues<CefrLevel>())
        {
            list.Add(new AchievementDefinition(
                $"level-{level}",
                $"{level} certified",
                $"Pass the {level} level exam.",
                "award",
                1,
                s => s.Certificates.ContainsKey(level) ? 1 : 0));
        }

        return list;
    }
}