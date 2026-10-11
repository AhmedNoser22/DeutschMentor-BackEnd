namespace DeutschMentor.Application.Drills.Dtos;

public sealed record ArticleDrillQuestionDto(Guid VocabularyItemId, string German, string English, string Arabic);

public sealed record ArticleDrillDto(IReadOnlyList<ArticleDrillQuestionDto> Questions);

public sealed record ArticleAnswerRequest(Guid VocabularyItemId, Article? Article);

public sealed record ArticleAnswerResultDto(
    bool Correct,
    Article CorrectArticle,
    string German,
    string ExampleGerman,
    string ExampleEnglish);