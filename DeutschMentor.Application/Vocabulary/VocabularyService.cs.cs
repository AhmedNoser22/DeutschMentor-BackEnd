namespace DeutschMentor.Application.Vocabulary;

public sealed class VocabularyService(
    IApplicationDbContext db,
    ILearningService learning,
    TimeProvider clock) : IVocabularyService
{
    public async Task<IReadOnlyList<VocabularyItemDto>> GetForLessonAsync(Guid userId, Guid lessonId, CancellationToken ct)
    {
        await learning.EnsureLessonAccessAsync(userId, lessonId, ct);

        var source = db.VocabularyItems.AsNoTracking()
            .Where(v => v.LessonId == lessonId)
            .OrderBy(v => v.Order);

        return await Project(source, userId).ToListAsync(ct);
    }

    public async Task<PagedResult<VocabularyItemDto>> SearchAsync(Guid userId, VocabularyQuery query, CancellationToken ct)
    {
        var lessonIds = (await learning.GetAccessibleLessonIdsAsync(userId, ct)).ToList();
        var source = db.VocabularyItems.AsNoTracking().Where(v => lessonIds.Contains(v.LessonId));

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            source = source.Where(v => v.German.Contains(term) || v.English.Contains(term) || v.Arabic.Contains(term));
        }

        if (query.Level is { } level)
        {
            source = source.Where(v => v.Lesson!.Unit!.Module!.Level!.Code == level);
        }

        if (query.PartOfSpeech is { } partOfSpeech)
        {
            source = source.Where(v => v.PartOfSpeech == partOfSpeech);
        }

        if (query.Article is { } article)
        {
            source = source.Where(v => v.Article == article);
        }

        var page = Math.Max(1, query.Page);
        var size = Math.Clamp(query.PageSize, 1, 50);
        var total = await source.CountAsync(ct);

        var ordered = source
            .OrderBy(v => v.Lesson!.Unit!.Module!.Level!.Order)
            .ThenBy(v => v.Lesson!.Unit!.Module!.Order)
            .ThenBy(v => v.Lesson!.Unit!.Order)
            .ThenBy(v => v.Lesson!.Order)
            .ThenBy(v => v.Order)
            .Skip((page - 1) * size)
            .Take(size);

        var items = await Project(ordered, userId).ToListAsync(ct);
        return new PagedResult<VocabularyItemDto>(items, total, page, size);
    }

    public async Task AddToReviewAsync(Guid userId, Guid vocabularyId, CancellationToken ct)
    {
        var lessonId = await db.VocabularyItems
            .Where(v => v.Id == vocabularyId)
            .Select(v => (Guid?)v.LessonId)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("word_not_found", "Word not found.");

        await learning.EnsureLessonAccessAsync(userId, lessonId, ct);

        var exists = await db.ReviewItems.AnyAsync(r => r.UserId == userId && r.VocabularyItemId == vocabularyId, ct);
        if (exists)
        {
            return;
        }

        db.ReviewItems.Add(new ReviewItem
        {
            UserId = userId,
            VocabularyItemId = vocabularyId,
            DueAtUtc = clock.GetUtcNow().UtcDateTime
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveFromReviewAsync(Guid userId, Guid vocabularyId, CancellationToken ct)
    {
        var item = await db.ReviewItems
            .FirstOrDefaultAsync(r => r.UserId == userId && r.VocabularyItemId == vocabularyId, ct);

        if (item is null)
        {
            return;
        }

        db.ReviewItems.Remove(item);
        await db.SaveChangesAsync(ct);
    }

    private IQueryable<VocabularyItemDto> Project(IQueryable<VocabularyItem> source, Guid userId) =>
        source.Select(v => new VocabularyItemDto(
            v.Id,
            v.LessonId,
            v.Lesson!.Unit!.Module!.Level!.Code,
            v.German,
            v.Article,
            v.PartOfSpeech,
            v.English,
            v.Arabic,
            v.ExampleGerman,
            v.ExampleEnglish,
            db.ReviewItems.Any(r => r.UserId == userId && r.VocabularyItemId == v.Id)));
}