namespace DeutschMentor.Application.Exercises;

internal sealed record GradeResult(bool IsCorrect, IReadOnlyList<string> Correct);

internal static class ExerciseGrader
{
    public const string PairSeparator = "::";

    public static ExerciseDto ToDto(Exercise exercise)
    {
        switch (exercise.Type)
        {
            case ExerciseType.MultipleChoice:
                return Build(exercise, options: Parse<MultipleChoicePayload>(exercise).Options);

            case ExerciseType.TrueFalse:
                return Build(exercise);

            case ExerciseType.FillBlank:
                return Build(exercise, blankCount: Parse<FillBlankPayload>(exercise).Blanks.Count);

            case ExerciseType.Ordering:
                return Build(exercise, words: Shuffled(Parse<OrderingPayload>(exercise).Solution));

            case ExerciseType.Matching:
                {
                    var pairs = Parse<MatchingPayload>(exercise).Pairs;
                    return Build(
                        exercise,
                        left: pairs.Select(p => p.Left).ToList(),
                        right: Shuffled(pairs.Select(p => p.Right).ToList()));
                }

            default:
                throw new InvalidOperationException($"Unsupported exercise type {exercise.Type}.");
        }
    }

    public static GradeResult Grade(Exercise exercise, IReadOnlyList<string> answer)
    {
        switch (exercise.Type)
        {
            case ExerciseType.MultipleChoice:
                {
                    var payload = Parse<MultipleChoicePayload>(exercise);
                    var correct = payload.Options[payload.CorrectIndex];
                    var ok = answer.Count == 1 && string.Equals(answer[0], correct, StringComparison.Ordinal);
                    return new GradeResult(ok, [correct]);
                }

            case ExerciseType.TrueFalse:
                {
                    var expected = Parse<TrueFalsePayload>(exercise).Correct ? "true" : "false";
                    var ok = answer.Count == 1 && string.Equals(answer[0], expected, StringComparison.OrdinalIgnoreCase);
                    return new GradeResult(ok, [expected]);
                }

            case ExerciseType.FillBlank:
                {
                    var blanks = Parse<FillBlankPayload>(exercise).Blanks;
                    var ok = answer.Count == blanks.Count
                        && blanks.Select((accepted, i) => accepted.Any(a => NormalizeText(a) == NormalizeText(answer[i]))).All(x => x);
                    return new GradeResult(ok, blanks.Select(b => b[0]).ToList());
                }

            case ExerciseType.Ordering:
                {
                    var solution = Parse<OrderingPayload>(exercise).Solution;
                    var ok = answer.SequenceEqual(solution, StringComparer.Ordinal);
                    return new GradeResult(ok, solution);
                }

            case ExerciseType.Matching:
                {
                    var pairs = Parse<MatchingPayload>(exercise).Pairs;
                    var expected = pairs.Select(p => $"{p.Left}{PairSeparator}{p.Right}").ToList();
                    var given = answer.ToHashSet(StringComparer.Ordinal);
                    var ok = answer.Count == expected.Count && given.Count == expected.Count && expected.All(given.Contains);
                    return new GradeResult(ok, expected);
                }

            default:
                throw new InvalidOperationException($"Unsupported exercise type {exercise.Type}.");
        }
    }

    private static ExerciseDto Build(
        Exercise exercise,
        IReadOnlyList<string>? options = null,
        IReadOnlyList<string>? words = null,
        IReadOnlyList<string>? left = null,
        IReadOnlyList<string>? right = null,
        int blankCount = 0) =>
        new(
            exercise.Id,
            exercise.Order,
            exercise.Type,
            exercise.Prompt,
            options ?? [],
            words ?? [],
            left ?? [],
            right ?? [],
            blankCount);

    private static T Parse<T>(Exercise exercise) where T : class =>
        JsonSerializer.Deserialize<T>(exercise.PayloadJson, JsonSerializerOptions.Web)
        ?? throw new InvalidOperationException($"Exercise {exercise.Id} has an invalid payload.");

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