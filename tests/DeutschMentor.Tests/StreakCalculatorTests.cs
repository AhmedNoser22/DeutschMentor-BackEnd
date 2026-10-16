using DeutschMentor.Application.Progress;

namespace DeutschMentor.Tests;

public class StreakCalculatorTests
{
    private static readonly DateOnly Today = new(2026, 10, 16);

    private static HashSet<DateOnly> Days(params int[] daysBack) => daysBack.Select(d => Today.AddDays(-d)).ToHashSet();

    [Fact]
    public void No_activity_means_no_streak()
    {
        var streak = StreakCalculator.Calculate(Days(), Today);
        Assert.Equal(0, streak.Current);
        Assert.Equal(0, streak.Longest);
        Assert.False(streak.StudiedToday);
    }

    [Fact]
    public void Consecutive_days_including_today()
    {
        var streak = StreakCalculator.Calculate(Days(0, 1, 2), Today);
        Assert.Equal(3, streak.Current);
        Assert.True(streak.StudiedToday);
    }

    [Fact]
    public void Streak_stays_alive_until_the_day_ends()
    {
        var streak = StreakCalculator.Calculate(Days(1, 2), Today);
        Assert.Equal(2, streak.Current);
        Assert.False(streak.StudiedToday);
    }

    [Fact]
    public void A_gap_breaks_the_current_streak_but_keeps_the_longest()
    {
        var streak = StreakCalculator.Calculate(Days(0, 2, 3), Today);
        Assert.Equal(1, streak.Current);
        Assert.Equal(2, streak.Longest);
    }

    [Fact]
    public void Old_activity_gives_no_current_streak()
    {
        var streak = StreakCalculator.Calculate(Days(3, 4, 5, 6), Today);
        Assert.Equal(0, streak.Current);
        Assert.Equal(4, streak.Longest);
    }
}