namespace DeutschMentor.Application.Vocabulary;

public interface IVocabularyService
{
    Task<IReadOnlyList<VocabularyItemDto>> GetForLessonAsync(Guid userId, Guid lessonId, CancellationToken ct);
    Task<PagedResult<VocabularyItemDto>> SearchAsync(Guid userId, VocabularyQuery query, CancellationToken ct);
    Task AddToReviewAsync(Guid userId, Guid vocabularyId, CancellationToken ct);
    Task RemoveFromReviewAsync(Guid userId, Guid vocabularyId, CancellationToken ct);
}