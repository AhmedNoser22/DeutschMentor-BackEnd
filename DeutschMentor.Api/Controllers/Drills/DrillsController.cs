namespace DeutschMentor.Api.Drills;

[ApiController]
[Authorize]
[Route("api/drills")]
public sealed class DrillsController(IDrillService drills) : ControllerBase
{
    [HttpGet("articles")]
    public async Task<ActionResult<ArticleDrillDto>> Articles(CancellationToken ct) =>
        Ok(await drills.GetArticleDrillAsync(User.GetUserId(), ct));

    [HttpPost("articles/answer")]
    public async Task<ActionResult<ArticleAnswerResultDto>> Answer(ArticleAnswerRequest request, CancellationToken ct) =>
        Ok(await drills.AnswerArticleAsync(User.GetUserId(), request, ct));
}