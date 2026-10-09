namespace DeutschMentor.Api.Content;

[ApiController]
[Authorize]
[Route("api/grammar")]
public sealed class GrammarController(IGrammarService grammar) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<GrammarSummaryDto>>> Search([FromQuery] GrammarQuery query, CancellationToken ct) =>
        Ok(await grammar.SearchAsync(User.GetUserId(), query, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GrammarTopicDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await grammar.GetAsync(User.GetUserId(), id, ct));

    [HttpGet("lesson/{lessonId:guid}")]
    public async Task<ActionResult<IReadOnlyList<GrammarTopicDto>>> ForLesson(Guid lessonId, CancellationToken ct) =>
        Ok(await grammar.GetForLessonAsync(User.GetUserId(), lessonId, ct));
}