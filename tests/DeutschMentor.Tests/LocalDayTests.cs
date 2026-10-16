using DeutschMentor.Application.Common;

namespace DeutschMentor.Tests;

public class LocalDayTests
{
    private static readonly DateTime Now = new(2026, 10, 16, 22, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void Local_day_starts_at_local_midnight()
    {
        Assert.Equal(new DateTime(2026, 10, 16, 21, 0, 0), LocalDay.StartUtc(Now, 180));
        Assert.Equal(new DateTime(2026, 10, 17, 21, 0, 0), LocalDay.EndUtc(Now, 180));
    }

    [Fact]
    public void Today_follows_the_local_clock()
    {
        Assert.Equal(new DateOnly(2026, 10, 17), LocalDay.Today(Now, 180));
        Assert.Equal(new DateOnly(2026, 10, 16), LocalDay.Today(Now, 0));
    }

    [Fact]
    public void Offsets_are_clamped() => Assert.Equal(840, LocalDay.ClampOffset(5000));
}