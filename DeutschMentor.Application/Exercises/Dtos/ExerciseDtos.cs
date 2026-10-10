namespace DeutschMentor.Application.Exercises.Dtos;

public sealed record ExerciseDto(
    Guid Id,
    int Order,
    ExerciseType Type,
    string Prompt,
    IReadOnlyList<string> Options,
    IReadOnlyList<string> Words,
    IReadOnlyList<string> Left,
    IReadOnlyList<string> Right,
    int BlankCount);

public sealed record ExerciseSetDto(IReadOnlyList<ExerciseDto> Exercises, IReadOnlyList<Guid> SolvedExerciseIds);

public sealed record SubmitAnswerRequest(List<string>? Answer);

public sealed record AnswerResultDto(
    bool IsCorrect,
    IReadOnlyList<string> CorrectAnswer,
    string ExplanationEn,
    string ExplanationAr);