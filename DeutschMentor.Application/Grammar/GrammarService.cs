namespace DeutschMentor.Application.Grammar;

public sealed class GrammarService(IApplicationDbContext db, ILearningService learning) : IGrammarService
{
    public async Task<IReadOnlyList<GrammarTopicDto>> GetForLessonAsync(Guid userId, Guid lessonId, CancellationToken ct)
    {
        await learning.EnsureLessonAccessAsync(userId, lessonId, ct);

        var rows = await db.GrammarTopics.AsNoTracking()
            .Where(t => t.LessonId == lessonId)
            .OrderBy(t => t.Order)
            .Select(t => new
            {
                t.Id,
                t.LessonId,
                Level = t.Lesson!.Unit!.Module!.Level!.Code,
                t.Title,
                t.Summary,
                t.ContentJson
            })
            .ToListAsync(ct);

        return rows
            .Select(r => new GrammarTopicDto(r.Id, r.LessonId, r.Level, r.Title, r.Summary, ParseContent(r.ContentJson)))
            .ToList();
    }

    public async Task<PagedResult<GrammarSummaryDto>> SearchAsync(Guid userId, GrammarQuery query, CancellationToken ct)
    {
        var lessonIds = (await learning.GetAccessibleLessonIdsAsync(userId, ct)).ToList();
        var source = db.GrammarTopics.AsNoTracking().Where(t => lessonIds.Contains(t.LessonId));

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            source = source.Where(t => t.Title.Contains(term) || t.Summary.Contains(term));
        }

        if (query.Level is { } level)
        {
            source = source.Where(t => t.Lesson!.Unit!.Module!.Level!.Code == level);
        }

        var page = Math.Max(1, query.Page);
        var size = Math.Clamp(query.PageSize, 1, 50);
        var total = await source.CountAsync(ct);

        var items = await source
            .OrderBy(t => t.Lesson!.Unit!.Module!.Level!.Order)
            .ThenBy(t => t.Lesson!.Unit!.Module!.Order)
            .ThenBy(t => t.Lesson!.Unit!.Order)
            .ThenBy(t => t.Lesson!.Order)
            .ThenBy(t => t.Order)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(t => new GrammarSummaryDto(
                t.Id,
                t.LessonId,
                t.Lesson!.Unit!.Module!.Level!.Code,
                t.Lesson.Title,
                t.Title,
                t.Summary))
            .ToListAsync(ct);

        return new PagedResult<GrammarSummaryDto>(items, total, page, size);
    }

    public async Task<GrammarTopicDto> GetAsync(Guid userId, Guid topicId, CancellationToken ct)
    {
        var row = await db.GrammarTopics.AsNoTracking()
            .Where(t => t.Id == topicId)
            .Select(t => new
            {
                t.Id,
                t.LessonId,
                Level = t.Lesson!.Unit!.Module!.Level!.Code,
                t.Title,
                t.Summary,
                t.ContentJson
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("grammar_not_found", "Grammar topic not found.");

        await learning.EnsureLessonAccessAsync(userId, row.LessonId, ct);
        return new GrammarTopicDto(row.Id, row.LessonId, row.Level, row.Title, row.Summary, ParseContent(row.ContentJson));
    }

    private static GrammarContent ParseContent(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<GrammarContent>(json, JsonSerializerOptions.Web)
                ?? new GrammarContent(null, null, null);
        }
        catch (JsonException)
        {
            return new GrammarContent(null, null, null);
        }
    }
}