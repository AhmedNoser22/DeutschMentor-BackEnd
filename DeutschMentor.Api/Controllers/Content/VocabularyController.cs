namespace DeutschMentor.Api.Content;

[ApiController]
[Authorize]
[Route("api/vocabulary")]
public sealed class VocabularyController(IVocabularyService vocabulary) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<VocabularyItemDto>>> Search([FromQuery] VocabularyQuery query, CancellationToken ct) =>
        Ok(await vocabulary.SearchAsync(User.GetUserId(), query, ct));

    [HttpGet("lesson/{lessonId:guid}")]
    public async Task<ActionResult<IReadOnlyList<VocabularyItemDto>>> ForLesson(Guid lessonId, CancellationToken ct) =>
        Ok(await vocabulary.GetForLessonAsync(User.GetUserId(), lessonId, ct));

    [HttpPost("{id:guid}/review")]
    public async Task<IActionResult> AddToReview(Guid id, CancellationToken ct)
    {
        await vocabulary.AddToReviewAsync(User.GetUserId(), id, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}/review")]
    public async Task<IActionResult> RemoveFromReview(Guid id, CancellationToken ct)
    {
        await vocabulary.RemoveFromReviewAsync(User.GetUserId(), id, ct);
        return NoContent();
    }
}