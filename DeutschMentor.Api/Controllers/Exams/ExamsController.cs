namespace DeutschMentor.Api.Exams;

[ApiController]
[Authorize]
[Route("api/exams")]
public sealed class ExamsController(IExamService exams) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ExamOverviewDto>> Overview(CancellationToken ct) =>
        Ok(await exams.GetOverviewAsync(User.GetUserId(), ct));

    [HttpPost("levels/{code}/start")]
    public async Task<ActionResult<ExamSessionDto>> StartLevel(string code, CancellationToken ct) =>
        Ok(await exams.StartLevelExamAsync(User.GetUserId(), code, ct));

    [HttpPost("placement/start")]
    public async Task<ActionResult<ExamSessionDto>> StartPlacement(CancellationToken ct) =>
        Ok(await exams.StartPlacementAsync(User.GetUserId(), ct));

    [HttpPost("placement/skip")]
    public async Task<IActionResult> SkipPlacement(CancellationToken ct)
    {
        await exams.SkipPlacementAsync(User.GetUserId(), ct);
        return NoContent();
    }

    [HttpGet("attempts/{id:guid}")]
    public async Task<ActionResult<ExamSessionDto>> Session(Guid id, CancellationToken ct) =>
        Ok(await exams.GetSessionAsync(User.GetUserId(), id, ct));

    [HttpPut("attempts/{id:guid}/answers/{questionId:guid}")]
    public async Task<IActionResult> SaveAnswer(Guid id, Guid questionId, SaveAnswerRequest request, CancellationToken ct)
    {
        await exams.SaveAnswerAsync(User.GetUserId(), id, questionId, request, ct);
        return NoContent();
    }

    [HttpPost("attempts/{id:guid}/submit")]
    public async Task<ActionResult<ExamResultDto>> Submit(Guid id, SubmitExamRequest request, CancellationToken ct) =>
        Ok(await exams.SubmitAsync(User.GetUserId(), id, request, ct));

    [HttpGet("attempts/{id:guid}/result")]
    public async Task<ActionResult<ExamResultDto>> Result(Guid id, CancellationToken ct) =>
        Ok(await exams.GetResultAsync(User.GetUserId(), id, ct));
}