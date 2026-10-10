namespace DeutschMentor.Application.Exercises;

internal static class ExerciseQueries
{
    public static async Task<List<Guid>> SolvedExerciseIdsAsync(
        this IApplicationDbContext db,
        Guid userId,
        List<Guid> exerciseIds,
        CancellationToken ct)
    {
        if (exerciseIds.Count == 0)
        {
            return [];
        }

        return await db.ExerciseAttempts.AsNoTracking()
            .Where(a => a.UserId == userId && a.IsCorrect && exerciseIds.Contains(a.ExerciseId))
            .Select(a => a.ExerciseId)
            .Distinct()
            .ToListAsync(ct);
    }
}