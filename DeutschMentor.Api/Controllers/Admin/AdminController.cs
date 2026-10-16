namespace DeutschMentor.Api.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin")]
public sealed class AdminController(IAdminService admin, IContentImportService importer, ILogger<AdminController> logger) : ControllerBase
{
    [HttpGet("stats")]
    public async Task<ActionResult<AdminStatsDto>> Stats(CancellationToken ct) => Ok(await admin.GetStatsAsync(ct));

    [HttpGet("lessons")]
    public async Task<ActionResult<PagedResult<AdminLessonDto>>> Lessons(
        [FromQuery] string? search, [FromQuery] string? level, [FromQuery] string? status, [FromQuery] int page, CancellationToken ct) =>
        Ok(await admin.GetLessonsAsync(search, level, status, page, ct));

    [HttpPost("lessons/{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id, CancellationToken ct)
    {
        await admin.SetStatusAsync(id, ContentStatus.Published, ct);
        return NoContent();
    }

    [HttpPost("lessons/{id:guid}/unpublish")]
    public async Task<IActionResult> Unpublish(Guid id, CancellationToken ct)
    {
        await admin.SetStatusAsync(id, ContentStatus.Draft, ct);
        return NoContent();
    }

    [HttpDelete("lessons/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await admin.DeleteLessonAsync(id, ct);
        return NoContent();
    }

    [HttpPost("import/check")]
    public ActionResult<ImportReport> Check(ContentPack pack) => Ok(importer.Check(pack));

    [HttpPost("import")]
    public async Task<ActionResult<ImportReport>> Import(ContentPack pack, [FromQuery] bool publish, CancellationToken ct)
    {
        var report = await importer.ImportAsync(pack, publish, ct);
        logger.LogInformation("Content import by {Admin}: level {Level}, imported {Imported}, errors {Errors}",
            User.GetUserId(), pack.Level, report.Imported, report.Errors.Count);
        return Ok(report);
    }

    [HttpGet("writing/pending")]
    public async Task<ActionResult<IReadOnlyList<AdminWritingDto>>> PendingWriting(CancellationToken ct) =>
        Ok(await admin.GetPendingWritingAsync(ct));

    [HttpPost("writing/{id:guid}/review")]
    public async Task<IActionResult> Review(Guid id, ReviewWritingRequest request, CancellationToken ct)
    {
        await admin.ReviewWritingAsync(id, request, User.GetUserId(), ct);
        return NoContent();
    }

    [HttpGet("exam-pool")]
    public async Task<ActionResult<IReadOnlyList<ExamPoolDto>>> ExamPool(CancellationToken ct) =>
        Ok(await admin.GetExamPoolAsync(ct));
}