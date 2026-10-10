namespace DeutschMentor.Application.Exercises;

public interface IExerciseService
{
    Task<ExerciseSetDto> GetForLessonAsync(Guid userId, Guid lessonId, CancellationToken ct);
    Task<AnswerResultDto> SubmitAsync(Guid userId, Guid exerciseId, SubmitAnswerRequest request, CancellationToken ct);
}