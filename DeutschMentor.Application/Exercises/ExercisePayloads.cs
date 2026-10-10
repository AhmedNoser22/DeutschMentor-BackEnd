namespace DeutschMentor.Application.Exercises;

internal sealed record MultipleChoicePayload(List<string> Options, int CorrectIndex);

internal sealed record TrueFalsePayload(bool Correct);

internal sealed record FillBlankPayload(List<List<string>> Blanks);

internal sealed record OrderingPayload(List<string> Solution);

internal sealed record MatchPair(string Left, string Right);

internal sealed record MatchingPayload(List<MatchPair> Pairs);