namespace DeutschMentor.Application.Progress;

internal static class ActivityReader
{
    private const int LookbackDays = 400;

    public static async Task<HashSet<DateOnly>> GetDaysAsync(
        IApplicationDbContext db,
        Guid userId,
        DateTime nowUtc,
        int offsetMinutes,
        CancellationToken ct)
    {
        var since = nowUtc.AddDays(-LookbackDays);
        double offset = offsetMinutes;
        var dates = new List<DateTime>();

        dates.AddRange(await db.ExerciseAttempts.AsNoTracking()
            .Where(a => a.UserId == userId && a.CreatedAtUtc >= since)
            .Select(a => a.CreatedAtUtc.AddMinutes(offset).Date)
            .Distinct()
            .ToListAsync(ct));

        dates.AddRange(await db.WritingSubmissions.AsNoTracking()
            .Where(a => a.UserId == userId && a.CreatedAtUtc >= since)
            .Select(a => a.CreatedAtUtc.AddMinutes(offset).Date)
            .Distinct()
            .ToListAsync(ct));

        dates.AddRange(await db.ReviewLogs.AsNoTracking()
            .Where(a => a.UserId == userId && a.CreatedAtUtc >= since)
            .Select(a => a.CreatedAtUtc.AddMinutes(offset).Date)
            .Distinct()
            .ToListAsync(ct));

        dates.AddRange(await db.DrillAttempts.AsNoTracking()
            .Where(a => a.UserId == userId && a.CreatedAtUtc >= since)
            .Select(a => a.CreatedAtUtc.AddMinutes(offset).Date)
            .Distinct()
            .ToListAsync(ct));

        dates.AddRange(await db.LessonProgress.AsNoTracking()
            .Where(p => p.UserId == userId && p.CreatedAtUtc >= since)
            .Select(p => p.CreatedAtUtc.AddMinutes(offset).Date)
            .Distinct()
            .ToListAsync(ct));

        dates.AddRange(await db.LessonProgress.AsNoTracking()
            .Where(p => p.UserId == userId && p.CompletedAtUtc != null && p.CompletedAtUtc >= since)
            .Select(p => p.CompletedAtUtc!.Value.AddMinutes(offset).Date)
            .Distinct()
            .ToListAsync(ct));

        dates.AddRange(await db.ExamAttempts.AsNoTracking()
            .Where(a => a.UserId == userId && a.StartedAtUtc >= since)
            .Select(a => a.StartedAtUtc.AddMinutes(offset).Date)
            .Distinct()
            .ToListAsync(ct));

        return dates.Select(DateOnly.FromDateTime).ToHashSet();
    }
}