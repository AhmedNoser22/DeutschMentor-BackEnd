namespace DeutschMentor.Application.Progress;

public interface IAchievementService
{
    Task<IReadOnlyList<AchievementDto>> GetAllAsync(Guid userId, int offsetMinutes, CancellationToken ct);
    Task<IReadOnlyList<AchievementDto>> EvaluateAsync(Guid userId, StreakDto streak, CancellationToken ct);
}