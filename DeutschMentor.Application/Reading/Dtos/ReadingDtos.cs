namespace DeutschMentor.Application.Reading.Dtos;

public sealed record ReadingLineDto(string? Speaker, string Text);

public sealed record ReadingPassageDto(
    Guid Id,
    string Title,
    IReadOnlyList<ReadingLineDto> Lines,
    IReadOnlyList<ExerciseDto> Questions,
    IReadOnlyList<Guid> SolvedQuestionIds);