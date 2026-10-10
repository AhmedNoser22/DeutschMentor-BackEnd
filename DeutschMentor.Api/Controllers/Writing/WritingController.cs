namespace DeutschMentor.Api.Writing;

[ApiController]
[Authorize]
[Route("api/writing")]
public sealed class WritingController(IWritingService writing) : ControllerBase
{
    [HttpGet("lesson/{lessonId:guid}")]
    public async Task<ActionResult<IReadOnlyList<WritingTaskDto>>> ForLesson(Guid lessonId, CancellationToken ct) =>
        Ok(await writing.GetForLessonAsync(User.GetUserId(), lessonId, ct));

    [HttpPost("tasks/{taskId:guid}/submissions")]
    public async Task<ActionResult<WritingSubmissionDto>> Submit(Guid taskId, SubmitWritingRequest request, CancellationToken ct) =>
        Ok(await writing.SubmitAsync(User.GetUserId(), taskId, request, ct));

    [HttpPost("submissions/{submissionId:guid}/request-review")]
    public async Task<ActionResult<WritingSubmissionDto>> RequestReview(Guid submissionId, CancellationToken ct) =>
        Ok(await writing.RequestReviewAsync(User.GetUserId(), submissionId, ct));
}