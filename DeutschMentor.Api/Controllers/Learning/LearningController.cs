namespace DeutschMentor.Api.Learning;

[ApiController]
[Authorize]
[Route("api/learning")]
public sealed class LearningController(ILearningService learning) : ControllerBase
{
    [HttpGet("levels")]
    public async Task<ActionResult<IReadOnlyList<LevelSummaryDto>>> Levels(CancellationToken ct) =>
        Ok(await learning.GetLevelsAsync(User.GetUserId(), ct));

    [HttpGet("levels/{code}")]
    public async Task<ActionResult<LevelDetailDto>> Level(string code, CancellationToken ct) =>
        Ok(await learning.GetLevelAsync(User.GetUserId(), code, ct));

    [HttpGet("lessons/{id:guid}")]
    public async Task<ActionResult<LessonDetailDto>> Lesson(Guid id, CancellationToken ct) =>
        Ok(await learning.GetLessonAsync(User.GetUserId(), id, ct));

    [HttpPost("lessons/{id:guid}/steps/{step}")]
    public async Task<ActionResult<LessonProgressDto>> MarkStep(Guid id, LessonStep step, CancellationToken ct) =>
        Ok(await learning.MarkStepSeenAsync(User.GetUserId(), id, step, ct));

    [HttpPost("lessons/{id:guid}/finish")]
    public async Task<ActionResult<FinishLessonResponse>> Finish(Guid id, CancellationToken ct) =>
        Ok(await learning.FinishLessonAsync(User.GetUserId(), id, ct));
}