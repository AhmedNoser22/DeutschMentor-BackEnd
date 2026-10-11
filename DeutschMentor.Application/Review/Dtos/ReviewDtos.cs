namespace DeutschMentor.Application.Review.Dtos;

public sealed record ReviewIntervalsDto(int Again, int Hard, int Good, int Easy);

public sealed record ReviewCardDto(
    Guid ReviewItemId,
    Guid VocabularyItemId,
    string German,
    Article? Article,
    PartOfSpeech PartOfSpeech,
    string English,
    string Arabic,
    string ExampleGerman,
    string ExampleEnglish,
    bool IsNew,
    ReviewIntervalsDto Intervals);

public sealed record ReviewQueueDto(IReadOnlyList<ReviewCardDto> Cards, int TotalDue, DateTime ServerNowUtc);

public sealed record ReviewSummaryDto(int DueCount, int EstimatedMinutes);

public sealed record RateReviewRequest(ReviewRating Rating);

public sealed record RateReviewResultDto(int IntervalDays, DateTime DueAtUtc, int DueRemaining);