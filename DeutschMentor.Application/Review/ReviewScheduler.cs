namespace DeutschMentor.Application.Review;

internal readonly record struct ReviewState(int IntervalDays, int Repetitions, double EaseFactor);

internal static class ReviewScheduler
{
    private const double MinEase = 1.3;
    private const double MaxEase = 3.0;
    private const int MaxIntervalDays = 365;

    public static ReviewState Next(ReviewState state, ReviewRating rating)
    {
        if (rating == ReviewRating.Again)
        {
            return new ReviewState(1, 0, Math.Max(MinEase, state.EaseFactor - 0.2));
        }

        var ease = Math.Clamp(
            rating switch
            {
                ReviewRating.Hard => state.EaseFactor - 0.15,
                ReviewRating.Easy => state.EaseFactor + 0.15,
                _ => state.EaseFactor
            },
            MinEase,
            MaxEase);

        int interval;
        if (state.Repetitions == 0)
        {
            interval = rating switch { ReviewRating.Hard => 2, ReviewRating.Good => 3, _ => 5 };
        }
        else if (state.Repetitions == 1)
        {
            interval = rating switch { ReviewRating.Hard => 3, ReviewRating.Good => 6, _ => 9 };
        }
        else
        {
            var current = Math.Max(1, state.IntervalDays);
            var raw = rating switch
            {
                ReviewRating.Hard => current * 1.2,
                ReviewRating.Easy => current * ease * 1.3,
                _ => current * ease
            };

            interval = Math.Max((int)Math.Round(raw, MidpointRounding.AwayFromZero), current + 1);
        }

        return new ReviewState(Math.Clamp(interval, 1, MaxIntervalDays), state.Repetitions + 1, ease);
    }

    public static ReviewIntervalsDto Preview(ReviewState state) => new(
        Next(state, ReviewRating.Again).IntervalDays,
        Next(state, ReviewRating.Hard).IntervalDays,
        Next(state, ReviewRating.Good).IntervalDays,
        Next(state, ReviewRating.Easy).IntervalDays);

    public static int EstimateMinutes(int cards) => cards <= 0 ? 0 : Math.Max(1, (int)Math.Ceiling(cards * 0.5));
}