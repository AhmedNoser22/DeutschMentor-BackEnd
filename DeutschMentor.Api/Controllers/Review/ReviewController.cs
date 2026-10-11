namespace DeutschMentor.Api.Review;

[ApiController]
[Authorize]
[Route("api/review")]
public sealed class ReviewController(IReviewService review) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<ReviewSummaryDto>> Summary(CancellationToken ct) =>
        Ok(await review.GetSummaryAsync(User.GetUserId(), Request.GetUtcOffsetMinutes(), ct));

    [HttpGet("queue")]
    public async Task<ActionResult<ReviewQueueDto>> Queue(CancellationToken ct) =>
        Ok(await review.GetQueueAsync(User.GetUserId(), Request.GetUtcOffsetMinutes(), ct));

    [HttpPost("{id:guid}/rate")]
    public async Task<ActionResult<RateReviewResultDto>> Rate(Guid id, RateReviewRequest request, CancellationToken ct) =>
        Ok(await review.RateAsync(User.GetUserId(), id, request.Rating, Request.GetUtcOffsetMinutes(), ct));
}