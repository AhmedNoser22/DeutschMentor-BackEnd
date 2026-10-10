namespace DeutschMentor.Application.Writing;

public interface IWritingService
{
    Task<IReadOnlyList<WritingTaskDto>> GetForLessonAsync(Guid userId, Guid lessonId, CancellationToken ct);
    Task<WritingSubmissionDto> SubmitAsync(Guid userId, Guid taskId, SubmitWritingRequest request, CancellationToken ct);
    Task<WritingSubmissionDto> RequestReviewAsync(Guid userId, Guid submissionId, CancellationToken ct);
}