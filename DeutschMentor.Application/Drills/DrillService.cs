namespace DeutschMentor.Application.Drills;

public sealed class DrillService(IApplicationDbContext db, ILearningService learning, TimeProvider clock) : IDrillService
{
    public async Task<ArticleDrillDto> GetArticleDrillAsync(Guid userId, CancellationToken ct)
    {
        var lessonIds = (await learning.GetAccessibleLessonIdsAsync(userId, ct)).ToList();

        var nouns = await db.VocabularyItems.AsNoTracking()
            .Where(v => lessonIds.Contains(v.LessonId) && v.Article != null)
            .Select(v => new ArticleDrillQuestionDto(v.Id, v.German, v.English, v.Arabic))
            .ToListAsync(ct);

        if (nouns.Count < DrillRules.MinWords)
        {
            throw new ConflictException(
                "drill_unavailable",
                $"Learn a few more nouns first. The drill needs at least {DrillRules.MinWords} words with articles.");
        }

        var since = clock.GetUtcNow().UtcDateTime.AddDays(-14);
        var missed = (await db.DrillAttempts.AsNoTracking()
            .Where(a => a.UserId == userId && !a.IsCorrect && a.CreatedAtUtc >= since)
            .Select(a => a.VocabularyItemId)
            .Distinct()
            .ToListAsync(ct)).ToHashSet();

        var shuffled = nouns.ToArray();
        Random.Shared.Shuffle(shuffled);

        var picked = shuffled
            .OrderByDescending(n => missed.Contains(n.VocabularyItemId))
            .Take(DrillRules.Size)
            .ToArray();

        Random.Shared.Shuffle(picked);
        return new ArticleDrillDto(picked);
    }

    public async Task<ArticleAnswerResultDto> AnswerArticleAsync(Guid userId, ArticleAnswerRequest request, CancellationToken ct)
    {
        if (request.Article is not { } chosen || !Enum.IsDefined(chosen))
        {
            throw new BadRequestException("invalid_answer", "Choose der, die or das.");
        }

        var word = await db.VocabularyItems.AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == request.VocabularyItemId && v.Article != null, ct)
            ?? throw new NotFoundException("word_not_found", "Word not found.");

        await learning.EnsureLessonAccessAsync(userId, word.LessonId, ct);

        var correct = word.Article == chosen;

        db.DrillAttempts.Add(new DrillAttempt
        {
            UserId = userId,
            VocabularyItemId = word.Id,
            ChosenArticle = chosen,
            IsCorrect = correct
        });
        await db.SaveChangesAsync(ct);

        return new ArticleAnswerResultDto(correct, word.Article!.Value, word.German, word.ExampleGerman, word.ExampleEnglish);
    }
}