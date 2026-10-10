namespace DeutschMentor.Api.Reading;

[ApiController]
[Authorize]
[Route("api/reading")]
public sealed class ReadingController(IReadingService reading) : ControllerBase
{
    [HttpGet("lesson/{lessonId:guid}")]
    public async Task<ActionResult<IReadOnlyList<ReadingPassageDto>>> ForLesson(Guid lessonId, CancellationToken ct) =>
        Ok(await reading.GetForLessonAsync(User.GetUserId(), lessonId, ct));
}