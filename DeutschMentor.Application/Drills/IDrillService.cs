namespace DeutschMentor.Application.Drills;

public interface IDrillService
{
    Task<ArticleDrillDto> GetArticleDrillAsync(Guid userId, CancellationToken ct);
    Task<ArticleAnswerResultDto> AnswerArticleAsync(Guid userId, ArticleAnswerRequest request, CancellationToken ct);
}