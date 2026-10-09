namespace DeutschMentor.Application.Grammar;

public interface IGrammarService
{
    Task<IReadOnlyList<GrammarTopicDto>> GetForLessonAsync(Guid userId, Guid lessonId, CancellationToken ct);
    Task<PagedResult<GrammarSummaryDto>> SearchAsync(Guid userId, GrammarQuery query, CancellationToken ct);
    Task<GrammarTopicDto> GetAsync(Guid userId, Guid topicId, CancellationToken ct);
}