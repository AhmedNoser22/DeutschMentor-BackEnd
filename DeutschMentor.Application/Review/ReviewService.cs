namespace DeutschMentor.Application.Review;

public sealed class ReviewService(IApplicationDbContext db, TimeProvider clock) : IReviewService
{
    private const int SessionSize = 50;

    public async Task<ReviewSummaryDto> GetSummaryAsync(Guid userId, int offsetMinutes, CancellationToken ct)
    {
        var cutoff = LocalDay.EndUtc(Now(), LocalDay.ClampOffset(offsetMinutes));
        var due = await CountDueAsync(userId, cutoff, ct);
        return new ReviewSummaryDto(due, ReviewScheduler.EstimateMinutes(due));
    }

    public async Task<ReviewQueueDto> GetQueueAsync(Guid userId, int offsetMinutes, CancellationToken ct)
    {
        var now = Now();
        var cutoff = LocalDay.EndUtc(now, LocalDay.ClampOffset(offsetMinutes));

        var rows = await (
                from r in db.ReviewItems.AsNoTracking()
                join v in db.VocabularyItems.AsNoTracking() on r.VocabularyItemId equals v.Id
                where r.UserId == userId && r.DueAtUtc < cutoff
                orderby r.DueAtUtc
                select new
                {
                    r.Id,
                    r.VocabularyItemId,
                    r.IntervalDays,
                    r.Repetitions,
                    r.EaseFactor,
                    v.German,
                    v.Article,
                    v.PartOfSpeech,
                    v.English,
                    v.Arabic,
                    v.ExampleGerman,
                    v.ExampleEnglish
                })
            .Take(SessionSize)
            .ToListAsync(ct);

        var total = await CountDueAsync(userId, cutoff, ct);

        var cards = rows
            .Select(x => new ReviewCardDto(
                x.Id,
                x.VocabularyItemId,
                x.German,
                x.Article,
                x.PartOfSpeech,
                x.English,
                x.Arabic,
                x.ExampleGerman,
                x.ExampleEnglish,
                x.Repetitions == 0 && x.IntervalDays == 0,
                ReviewScheduler.Preview(new ReviewState(x.IntervalDays, x.Repetitions, x.EaseFactor))))
            .ToList();

        return new ReviewQueueDto(cards, total, now);
    }

    public async Task<RateReviewResultDto> RateAsync(
        Guid userId,
        Guid reviewItemId,
        ReviewRating rating,
        int offsetMinutes,
        CancellationToken ct)
    {
        if (!Enum.IsDefined(rating))
        {
            throw new BadRequestException("invalid_rating", "Choose Again, Hard, Good or Easy.");
        }

        var item = await db.ReviewItems.FirstOrDefaultAsync(r => r.Id == reviewItemId && r.UserId == userId, ct)
            ?? throw new NotFoundException("review_item_not_found", "Review card not found.");

        var now = Now();
        var cutoff = LocalDay.EndUtc(now, LocalDay.ClampOffset(offsetMinutes));

        if (item.DueAtUtc >= cutoff)
        {
            var remainingNow = await CountDueAsync(userId, cutoff, ct);
            return new RateReviewResultDto(item.IntervalDays, item.DueAtUtc.AsUtc(), remainingNow);
        }

        var next = ReviewScheduler.Next(new ReviewState(item.IntervalDays, item.Repetitions, item.EaseFactor), rating);

        item.IntervalDays = next.IntervalDays;
        item.Repetitions = next.Repetitions;
        item.EaseFactor = Math.Round(next.EaseFactor, 3);
        item.DueAtUtc = now.AddDays(next.IntervalDays);

        db.ReviewLogs.Add(new ReviewLog
        {
            UserId = userId,
            VocabularyItemId = item.VocabularyItemId,
            Rating = rating,
            IntervalDaysAfter = item.IntervalDays,
            EaseFactorAfter = item.EaseFactor
        });

        await db.SaveChangesAsync(ct);

        var remaining = await CountDueAsync(userId, cutoff, ct);
        return new RateReviewResultDto(item.IntervalDays, item.DueAtUtc.AsUtc(), remaining);
    }

    private DateTime Now() => clock.GetUtcNow().UtcDateTime;

    private Task<int> CountDueAsync(Guid userId, DateTime cutoffUtc, CancellationToken ct) =>
        db.ReviewItems.CountAsync(r => r.UserId == userId && r.DueAtUtc < cutoffUtc, ct);
}