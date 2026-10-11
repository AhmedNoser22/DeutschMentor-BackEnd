namespace DeutschMentor.Application.Progress;

internal sealed record MentorFacts(
    int DueCount,
    int StreakCurrent,
    bool StudiedToday,
    int Attempts7,
    int Correct7,
    int ArticleMisses7,
    bool HasDrill,
    int DrillMinutes,
    string? FailedExamLevel,
    IReadOnlyList<string> FailedExamWeakSkills,
    GermanDay? Holiday);

internal static class MentorAdvisor
{
    public static IReadOnlyList<MentorMessageDto> Build(MentorFacts facts)
    {
        var advice = new List<MentorMessageDto>();

        if (facts.HasDrill && facts.ArticleMisses7 >= 3)
        {
            advice.Add(new MentorMessageDto(
                "Articles need work.",
                $"You missed der, die or das {facts.ArticleMisses7} times this week. Today's plan has a {facts.DrillMinutes}-minute drill."));
        }

        if (facts.FailedExamLevel is not null && facts.FailedExamWeakSkills.Count > 0)
        {
            advice.Add(new MentorMessageDto(
                $"Your {facts.FailedExamLevel} exam needs a second look.",
                $"{string.Join(", ", facts.FailedExamWeakSkills)} scored below the pass mark. Re-open those parts of your {facts.FailedExamLevel} lessons before you retake it."));
        }

        if (facts.Attempts7 >= 8)
        {
            var percent = (int)Math.Round(100.0 * facts.Correct7 / facts.Attempts7, MidpointRounding.AwayFromZero);
            if (percent < 60)
            {
                advice.Add(new MentorMessageDto(
                    "Practice answers are slipping.",
                    $"You got {percent}% of your practice answers right in the last 7 days. Re-read the grammar tab before the next exercises."));
            }
        }

        if (facts.DueCount >= 15)
        {
            advice.Add(new MentorMessageDto(
                "Your review pile is growing.",
                $"{facts.DueCount} cards are due. A few minutes now stops them from piling up."));
        }

        if (!facts.StudiedToday && facts.StreakCurrent >= 2)
        {
            advice.Add(new MentorMessageDto(
                $"Keep your {facts.StreakCurrent}-day streak.",
                "Even five minutes today counts. Start with your due cards."));
        }

        if (facts.StudiedToday && facts.StreakCurrent >= 3)
        {
            advice.Add(new MentorMessageDto(
                $"{facts.StreakCurrent} days in a row.",
                "Nice rhythm. Short daily sessions beat long weekly ones."));
        }

        if (advice.Count == 0 && facts.StreakCurrent == 0 && facts.Attempts7 == 0)
        {
            advice.Add(new MentorMessageDto(
                "Welcome to DeutschMentor.",
                "Start with today's lesson, then add its words to your review."));
        }

        if (advice.Count == 0)
        {
            advice.Add(new MentorMessageDto("You are on track.", "Finish today's plan to keep your words fresh."));
        }

        var result = advice.Take(facts.Holiday is null ? 2 : 1).ToList();

        if (facts.Holiday is { } day)
        {
            result.Add(new MentorMessageDto($"Today is {day.Name}.", day.Note));
        }

        return result;
    }

    public static List<string> WeakSkills(string breakdownJson, int passMark)
    {
        try
        {
            var items = System.Text.Json.JsonSerializer.Deserialize<List<ScoreBreakdownDto>>(
                breakdownJson,
                System.Text.Json.JsonSerializerOptions.Web) ?? [];

            return items.Where(i => i.Percent < passMark).Select(i => i.Label).ToList();
        }
        catch (System.Text.Json.JsonException)
        {
            return [];
        }
    }
}