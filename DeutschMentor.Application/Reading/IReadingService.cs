namespace DeutschMentor.Application.Reading;

public interface IReadingService
{
    Task<IReadOnlyList<ReadingPassageDto>> GetForLessonAsync(Guid userId, Guid lessonId, CancellationToken ct);
}