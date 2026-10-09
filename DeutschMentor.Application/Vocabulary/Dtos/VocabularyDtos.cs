namespace DeutschMentor.Application.Vocabulary.Dtos;

public sealed record VocabularyItemDto(
    Guid Id,
    Guid LessonId,
    CefrLevel Level,
    string German,
    Article? Article,
    PartOfSpeech PartOfSpeech,
    string English,
    string Arabic,
    string ExampleGerman,
    string ExampleEnglish,
    bool InReview);

public sealed class VocabularyQuery
{
    public string? Search { get; init; }
    public CefrLevel? Level { get; init; }
    public PartOfSpeech? PartOfSpeech { get; init; }
    public Article? Article { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}