namespace DeutschMentor.Application.Progress;

public interface IProgressService
{
    Task<TodayDto> GetTodayAsync(Guid userId, int offsetMinutes, CancellationToken ct);
}