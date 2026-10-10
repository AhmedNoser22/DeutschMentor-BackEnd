namespace DeutschMentor.Application.Reading;

public sealed class ReadingService(IApplicationDbContext db, ILearningService learning) : IReadingService
{
    public async Task<IReadOnlyList<ReadingPassageDto>> GetForLessonAsync(Guid userId, Guid lessonId, CancellationToken ct)
    {
        await learning.EnsureLessonAccessAsync(userId, lessonId, ct);

        var passages = await db.ReadingPassages.AsNoTracking()
            .Where(p => p.LessonId == lessonId)
            .OrderBy(p => p.Order)
            .ToListAsync(ct);

        if (passages.Count == 0)
        {
            return [];
        }

        var passageIds = passages.Select(p => p.Id).ToList();
        var questions = await db.Exercises.AsNoTracking()
            .Where(e => e.ReadingPassageId != null && passageIds.Contains(e.ReadingPassageId.Value))
            .OrderBy(e => e.Order)
            .ToListAsync(ct);

        var solved = (await db.SolvedExerciseIdsAsync(userId, questions.Select(q => q.Id).ToList(), ct)).ToHashSet();

        return passages
            .Select(p =>
            {
                var own = questions.Where(q => q.ReadingPassageId == p.Id).ToList();
                return new ReadingPassageDto(
                    p.Id,
                    p.Title,
                    ParseLines(p.BodyJson),
                    own.Select(ExerciseGrader.ToDto).ToList(),
                    own.Where(q => solved.Contains(q.Id)).Select(q => q.Id).ToList());
            })
            .ToList();
    }

    private static List<ReadingLineDto> ParseLines(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<ReadingLineDto>>(json, JsonSerializerOptions.Web) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}