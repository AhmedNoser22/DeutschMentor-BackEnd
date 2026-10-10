namespace DeutschMentor.Application.Exercises;

public sealed class ExerciseService(IApplicationDbContext db, ILearningService learning) : IExerciseService
{
    public async Task<ExerciseSetDto> GetForLessonAsync(Guid userId, Guid lessonId, CancellationToken ct)
    {
        await learning.EnsureLessonAccessAsync(userId, lessonId, ct);

        var exercises = await db.Exercises.AsNoTracking()
            .Where(e => e.LessonId == lessonId && e.ReadingPassageId == null)
            .OrderBy(e => e.Order)
            .ToListAsync(ct);

        var solved = await db.SolvedExerciseIdsAsync(userId, exercises.Select(e => e.Id).ToList(), ct);
        return new ExerciseSetDto(exercises.Select(ExerciseGrader.ToDto).ToList(), solved);
    }

    public async Task<AnswerResultDto> SubmitAsync(Guid userId, Guid exerciseId, SubmitAnswerRequest request, CancellationToken ct)
    {
        var exercise = await db.Exercises.AsNoTracking().FirstOrDefaultAsync(e => e.Id == exerciseId, ct)
            ?? throw new NotFoundException("exercise_not_found", "Exercise not found.");

        await learning.EnsureLessonAccessAsync(userId, exercise.LessonId, ct);

        var answer = request.Answer;
        if (answer is null || answer.Count == 0 || answer.Count > 30 || answer.Any(a => a is null || a.Length > 200))
        {
            throw new BadRequestException("invalid_answer", "The answer is not valid.");
        }

        var grade = ExerciseGrader.Grade(exercise, answer);

        db.ExerciseAttempts.Add(new ExerciseAttempt
        {
            UserId = userId,
            ExerciseId = exercise.Id,
            AnswerJson = JsonSerializer.Serialize(answer),
            IsCorrect = grade.IsCorrect
        });
        await db.SaveChangesAsync(ct);

        return new AnswerResultDto(grade.IsCorrect, grade.Correct, exercise.ExplanationEn, exercise.ExplanationAr);
    }
}