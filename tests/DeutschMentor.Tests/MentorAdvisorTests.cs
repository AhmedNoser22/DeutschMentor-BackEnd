using DeutschMentor.Application.Progress;

namespace DeutschMentor.Tests;

public class MentorAdvisorTests
{
    private static MentorFacts Facts(
        int due = 0, int streak = 0, bool studied = false, int attempts = 0, int correct = 0,
        int misses = 0, GermanDay? holiday = null) =>
        new(due, streak, studied, attempts, correct, misses, true, 6, null, [], holiday);

    [Fact]
    public void New_user_gets_a_welcome() =>
        Assert.Equal("Welcome to DeutschMentor.", Assert.Single(MentorAdvisor.Build(Facts())).Title);

    [Fact]
    public void Article_mistakes_come_first() =>
        Assert.Equal("Articles need work.", MentorAdvisor.Build(Facts(misses: 4))[0].Title);

    [Fact]
    public void Large_review_pile_is_flagged() =>
        Assert.Contains(MentorAdvisor.Build(Facts(due: 20, studied: true)), m => m.Title == "Your review pile is growing.");

    [Fact]
    public void Streak_at_risk_is_flagged() =>
        Assert.Contains(MentorAdvisor.Build(Facts(streak: 5)), m => m.Title == "Keep your 5-day streak.");

    [Fact]
    public void German_holiday_is_added_as_the_last_message()
    {
        var holiday = GermanDays.For(new DateOnly(2026, 10, 3));
        var result = MentorAdvisor.Build(Facts(misses: 4, holiday: holiday));

        Assert.Equal(2, result.Count);
        Assert.Equal("Today is Tag der Deutschen Einheit.", result[^1].Title);
    }
}