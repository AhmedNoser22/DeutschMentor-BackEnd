namespace DeutschMentor.Application.Review;

public interface IReviewService
{
    Task<ReviewSummaryDto> GetSummaryAsync(Guid userId, int offsetMinutes, CancellationToken ct);
    Task<ReviewQueueDto> GetQueueAsync(Guid userId, int offsetMinutes, CancellationToken ct);
    Task<RateReviewResultDto> RateAsync(Guid userId, Guid reviewItemId, ReviewRating rating, int offsetMinutes, CancellationToken ct);
}