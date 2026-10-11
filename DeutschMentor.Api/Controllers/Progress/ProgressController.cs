namespace DeutschMentor.Api.Progress;

[ApiController]
[Authorize]
[Route("api/progress")]
public sealed class ProgressController(IProgressService progress, IAchievementService achievements) : ControllerBase
{
    [HttpGet("today")]
    public async Task<ActionResult<TodayDto>> Today(CancellationToken ct) =>
        Ok(await progress.GetTodayAsync(User.GetUserId(), Request.GetUtcOffsetMinutes(), ct));

    [HttpGet("achievements")]
    public async Task<ActionResult<IReadOnlyList<AchievementDto>>> Achievements(CancellationToken ct) =>
        Ok(await achievements.GetAllAsync(User.GetUserId(), Request.GetUtcOffsetMinutes(), ct));
}