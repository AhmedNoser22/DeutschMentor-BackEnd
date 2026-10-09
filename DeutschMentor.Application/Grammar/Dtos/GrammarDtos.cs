namespace DeutschMentor.Application.Grammar.Dtos;

public sealed record GrammarTable(
    IReadOnlyList<string> Headers,
    IReadOnlyList<IReadOnlyList<string>> Rows,
    IReadOnlyList<int> HighlightColumns);

public sealed record GrammarContent(GrammarTable? Table, string? NoteEn, string? NoteAr);

public sealed record GrammarTopicDto(
    Guid Id,
    Guid LessonId,
    CefrLevel Level,
    string Title,
    string Summary,
    GrammarContent Content);

public sealed record GrammarSummaryDto(
    Guid Id,
    Guid LessonId,
    CefrLevel Level,
    string LessonTitle,
    string Title,
    string Summary);

public sealed class GrammarQuery
{
    public string? Search { get; init; }
    public CefrLevel? Level { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}