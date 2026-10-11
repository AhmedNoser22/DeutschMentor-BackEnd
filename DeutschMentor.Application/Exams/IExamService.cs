namespace DeutschMentor.Application.Exams;

public interface IExamService
{
    Task<ExamOverviewDto> GetOverviewAsync(Guid userId, CancellationToken ct);
    Task<ExamSessionDto> StartLevelExamAsync(Guid userId, string levelCode, CancellationToken ct);
    Task<ExamSessionDto> StartPlacementAsync(Guid userId, CancellationToken ct);
    Task SkipPlacementAsync(Guid userId, CancellationToken ct);
    Task<ExamSessionDto> GetSessionAsync(Guid userId, Guid attemptId, CancellationToken ct);
    Task SaveAnswerAsync(Guid userId, Guid attemptId, Guid questionId, SaveAnswerRequest request, CancellationToken ct);
    Task<ExamResultDto> SubmitAsync(Guid userId, Guid attemptId, SubmitExamRequest request, CancellationToken ct);
    Task<ExamResultDto> GetResultAsync(Guid userId, Guid attemptId, CancellationToken ct);
}