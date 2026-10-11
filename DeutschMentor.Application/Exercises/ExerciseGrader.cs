namespace DeutschMentor.Application.Exercises;

internal sealed record GradeResult(bool IsCorrect, IReadOnlyList<string> Correct);

internal static class ExerciseGrader
{
    public const string PairSeparator = "::";

    public static ExerciseDto ToDto(Exercise exercise) =>
        ToDto(exercise.Id, exercise.Order, exercise.Type, exercise.Prompt, exercise.PayloadJson);

    public static ExerciseDto ToDto(Guid id, int order, ExerciseType type, string prompt, string payloadJson)
    {
        switch (type)
        {
            case ExerciseType.MultipleChoice:
                return Build(id, order, type, prompt, options: Parse<MultipleChoicePayload>(payloadJson).Options);

            case ExerciseType.TrueFalse:
                return Build(id, order, type, prompt);

            case ExerciseType.FillBlank:
                return Build(id, order, type, prompt, blankCount: Parse<FillBlankPayload>(payloadJson).Blanks.Count);

            case ExerciseType.Ordering:
                return Build(id, order, type, prompt, words: Shuffled(Parse<OrderingPayload>(payloadJson).Solution));

            case ExerciseType.Matching:
                {
                    var pairs = Parse<MatchingPayload>(payloadJson).Pairs;
                    return Build(
                        id,
                        order,
                        type,
                        prompt,
                        left: pairs.Select(p => p.Left).ToList(),
                        right: Shuffled(pairs.Select(p => p.Right).ToList()));
                }

            default:
                throw new InvalidOperationException($"Unsupported exercise type {type}.");
        }
    }

    public static GradeResult Grade(Exercise exercise, IReadOnlyList<string> answer) =>
        Grade(exercise.Type, exercise.PayloadJson, answer);

    public static GradeResult Grade(ExerciseType type, string payloadJson, IReadOnlyList<string> answer)
    {
        switch (type)
        {
            case ExerciseType.MultipleChoice:
                {
                    var payload = Parse<MultipleChoicePayload>(payloadJson);
                    var correct = payload.Options[payload.CorrectIndex];
                    var ok = answer.Count == 1 && string.Equals(answer[0], correct, StringComparison.Ordinal);
                    return new GradeResult(ok, [correct]);
                }

            case ExerciseType.TrueFalse:
                {
                    var expected = Parse<TrueFalsePayload>(payloadJson).Correct ? "true" : "false";
                    var ok = answer.Count == 1 && string.Equals(answer[0], expected, StringComparison.OrdinalIgnoreCase);
                    return new GradeResult(ok, [expected]);
                }

            case ExerciseType.FillBlank:
                {
                    var blanks = Parse<FillBlankPayload>(payloadJson).Blanks;
                    var ok = answer.Count == blanks.Count
                        && blanks.Select((accepted, i) => accepted.Any(a => NormalizeText(a) == NormalizeText(answer[i]))).All(x => x);
                    return new GradeResult(ok, blanks.Select(b => b[0]).ToList());
                }

            case ExerciseType.Ordering:
                {
                    var solution = Parse<OrderingPayload>(payloadJson).Solution;
                    var ok = answer.SequenceEqual(solution, StringComparer.Ordinal);
                    return new GradeResult(ok, solution);
                }

            case ExerciseType.Matching:
                {
                    var pairs = Parse<MatchingPayload>(payloadJson).Pairs;
                    var expected = pairs.Select(p => $"{p.Left}{PairSeparator}{p.Right}").ToList();
                    var given = answer.ToHashSet(StringComparer.Ordinal);
                    var ok = answer.Count == expected.Count && given.Count == expected.Count && expected.All(given.Contains);
                    return new GradeResult(ok, expected);
                }

            default:
                throw new InvalidOperationException($"Unsupported exercise type {type}.");
        }
    }

    private static ExerciseDto Build(
        Guid id,
        int order,
        ExerciseType type,
        string prompt,
        IReadOnlyList<string>? options = null,
        IReadOnlyList<string>? words = null,
        IReadOnlyList<string>? left = null,
        IReadOnlyList<string>? right = null,
        int blankCount = 0) =>
        new(
            id,
            order,
            type,
            prompt,
            options ?? [],
            words ?? [],
            left ?? [],
            right ?? [],
            blankCount);

    private static T Parse<T>(string json) where T : class =>
        JsonSerializer.Deserialize<T>(json, JsonSerializerOptions.Web)
        ?? throw new InvalidOperationException("The exercise payload is not valid.");

    private static List<string> Shuffled(IReadOnlyList<string> source)
    {
        var copy = source.ToArray();
        if (copy.Length < 2)
        {
            return [.. copy];
        }

        for (var attempt = 0; attempt < 5; attempt++)
        {
            Random.Shared.Shuffle(copy);
            if (!copy.SequenceEqual(source))
            {
                break;
            }
        }

        return [.. copy];
    }

    private static string NormalizeText(string value) =>
        string.Join(' ', value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToLowerInvariant();
}