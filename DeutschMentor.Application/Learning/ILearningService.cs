namespace DeutschMentor.Application.Learning;

public interface ILearningService
{
    Task<IReadOnlyList<LevelSummaryDto>> GetLevelsAsync(Guid userId, CancellationToken ct);
    Task<LevelDetailDto> GetLevelAsync(Guid userId, string code, CancellationToken ct);
    Task<LessonDetailDto> GetLessonAsync(Guid userId, Guid lessonId, CancellationToken ct);
    Task<LessonProgressDto> MarkStepSeenAsync(Guid userId, Guid lessonId, LessonStep step, CancellationToken ct);
    Task<FinishLessonResponse> FinishLessonAsync(Guid userId, Guid lessonId, CancellationToken ct);
    Task EnsureLessonAccessAsync(Guid userId, Guid lessonId, CancellationToken ct);
    Task<IReadOnlySet<Guid>> GetAccessibleLessonIdsAsync(Guid userId, CancellationToken ct);
}