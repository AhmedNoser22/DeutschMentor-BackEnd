namespace DeutschMentor.Api.Exercises;

[ApiController]
[Authorize]
[Route("api/exercises")]
public sealed class ExercisesController(IExerciseService exercises) : ControllerBase
{
    [HttpGet("lesson/{lessonId:guid}")]
    public async Task<ActionResult<ExerciseSetDto>> ForLesson(Guid lessonId, CancellationToken ct) =>
        Ok(await exercises.GetForLessonAsync(User.GetUserId(), lessonId, ct));

    [HttpPost("{id:guid}/answer")]
    public async Task<ActionResult<AnswerResultDto>> Answer(Guid id, SubmitAnswerRequest request, CancellationToken ct) =>
        Ok(await exercises.SubmitAsync(User.GetUserId(), id, request, ct));
}