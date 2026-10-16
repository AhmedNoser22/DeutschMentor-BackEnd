using DeutschMentor.Application.Review;
using DeutschMentor.Application.Review.Dtos;
using DeutschMentor.Domain.Enums;

namespace DeutschMentor.Tests;

public class ReviewSchedulerTests
{
    [Fact]
    public void Again_resets_the_card_and_lowers_the_ease()
    {
        var next = ReviewScheduler.Next(new ReviewState(10, 3, 2.5), ReviewRating.Again);

        Assert.Equal(1, next.IntervalDays);
        Assert.Equal(0, next.Repetitions);
        Assert.Equal(2.3, next.EaseFactor, 3);
    }

    [Theory]
    [InlineData(ReviewRating.Hard, 2)]
    [InlineData(ReviewRating.Good, 3)]
    [InlineData(ReviewRating.Easy, 5)]
    public void First_success_uses_fixed_steps(ReviewRating rating, int days) =>
        Assert.Equal(days, ReviewScheduler.Next(new ReviewState(0, 0, 2.5), rating).IntervalDays);

    [Fact]
    public void Second_good_review_gives_six_days() =>
        Assert.Equal(6, ReviewScheduler.Next(new ReviewState(3, 1, 2.5), ReviewRating.Good).IntervalDays);

    [Theory]
    [InlineData(ReviewRating.Hard, 7)]
    [InlineData(ReviewRating.Good, 15)]
    [InlineData(ReviewRating.Easy, 21)]
    public void Later_reviews_multiply_by_the_ease(ReviewRating rating, int days) =>
        Assert.Equal(days, ReviewScheduler.Next(new ReviewState(6, 2, 2.5), rating).IntervalDays);

    [Fact]
    public void Ease_never_drops_below_the_floor()
    {
        var state = new ReviewState(1, 0, 2.5);
        for (var i = 0; i < 20; i++)
        {
            state = ReviewScheduler.Next(state, ReviewRating.Again);
        }

        Assert.Equal(1.3, state.EaseFactor, 3);
    }

    [Fact]
    public void Ease_never_rises_above_the_ceiling()
    {
        var state = new ReviewState(1, 2, 2.5);
        for (var i = 0; i < 20; i++)
        {
            state = ReviewScheduler.Next(state, ReviewRating.Easy);
        }

        Assert.Equal(3.0, state.EaseFactor, 3);
        Assert.InRange(state.IntervalDays, 1, 365);
    }

    [Fact]
    public void Preview_for_a_new_card_matches_the_buttons() =>
        Assert.Equal(new ReviewIntervalsDto(1, 2, 3, 5), ReviewScheduler.Preview(new ReviewState(0, 0, 2.5)));
}