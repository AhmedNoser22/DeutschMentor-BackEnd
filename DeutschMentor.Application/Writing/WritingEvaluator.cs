namespace DeutschMentor.Application.Writing;

internal sealed record WritingGroup(string Label, List<string>? AnyOf);

internal sealed record WritingRules(List<WritingGroup>? Groups);

internal sealed record WritingEvaluation(int ScorePercent, bool Passed, int WordCount, List<WritingCheckDto> Checks);

internal static class WritingEvaluator
{
    public static WritingEvaluation Evaluate(WritingTask task, string text)
    {
        var groups = ParseGroups(task.RulesJson);
        var normalized = Normalize(text);
        var wordCount = normalized.Length == 0 ? 0 : normalized.Split(' ').Length;
        var padded = $" {normalized} ";

        var checks = new List<WritingCheckDto>();
        var met = 0;

        foreach (var group in groups)
        {
            var hit = (group.AnyOf ?? []).Any(variant =>
            {
                var phrase = Normalize(variant);
                return phrase.Length > 0 && padded.Contains($" {phrase} ", StringComparison.Ordinal);
            });

            if (hit)
            {
                met++;
            }

            checks.Add(new WritingCheckDto(group.Label, hit));
        }

        var coverage = groups.Count == 0 ? 1.0 : met / (double)groups.Count;
        var lengthScore = task.MinWords <= 0 ? 1.0 : Math.Min(1.0, wordCount / (double)task.MinWords);

        if (task.MinWords > 0)
        {
            checks.Add(new WritingCheckDto($"At least {task.MinWords} words", wordCount >= task.MinWords));
        }

        var score = (int)Math.Round(100 * (0.8 * coverage + 0.2 * lengthScore), MidpointRounding.AwayFromZero);
        return new WritingEvaluation(score, score >= task.PassPercent, wordCount, checks);
    }

    private static List<WritingGroup> ParseGroups(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<WritingRules>(json, JsonSerializerOptions.Web)?.Groups ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string Normalize(string value)
    {
        var builder = new StringBuilder();
        var lastWasSpace = true;

        foreach (var ch in value.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
                lastWasSpace = false;
            }
            else if (!lastWasSpace)
            {
                builder.Append(' ');
                lastWasSpace = true;
            }
        }

        return builder.ToString().Trim();
    }
}