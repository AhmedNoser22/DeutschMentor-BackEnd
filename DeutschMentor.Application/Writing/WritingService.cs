namespace DeutschMentor.Application.Writing;

public sealed class WritingService(IApplicationDbContext db, ILearningService learning) : IWritingService
{
    private const int MaxLength = 2000;

    public async Task<IReadOnlyList<WritingTaskDto>> GetForLessonAsync(Guid userId, Guid lessonId, CancellationToken ct)
    {
        await learning.EnsureLessonAccessAsync(userId, lessonId, ct);

        var tasks = await db.WritingTasks.AsNoTracking()
            .Where(t => t.LessonId == lessonId)
            .OrderBy(t => t.Order)
            .ToListAsync(ct);

        if (tasks.Count == 0)
        {
            return [];
        }

        var taskIds = tasks.Select(t => t.Id).ToList();
        var submissions = await db.WritingSubmissions.AsNoTracking()
            .Where(s => s.UserId == userId && taskIds.Contains(s.WritingTaskId))
            .OrderByDescending(s => s.CreatedAtUtc)
            .ToListAsync(ct);

        var latest = submissions
            .GroupBy(s => s.WritingTaskId)
            .ToDictionary(g => g.Key, g => g.First());

        return tasks
            .Select(t => new WritingTaskDto(
                t.Id,
                t.Order,
                t.Title,
                t.Prompt,
                t.MinWords,
                t.PassPercent,
                latest.TryGetValue(t.Id, out var submission) ? ToDto(submission, t) : null))
            .ToList();
    }

    public async Task<WritingSubmissionDto> SubmitAsync(Guid userId, Guid taskId, SubmitWritingRequest request, CancellationToken ct)
    {
        var task = await db.WritingTasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == taskId, ct)
            ?? throw new NotFoundException("writing_task_not_found", "Writing task not found.");

        await learning.EnsureLessonAccessAsync(userId, task.LessonId, ct);

        var text = (request.Text ?? string.Empty).Trim();
        if (text.Length == 0)
        {
            throw new BadRequestException("empty_text", "Write something first.");
        }

        if (text.Length > MaxLength)
        {
            throw new BadRequestException("text_too_long", $"Keep your text under {MaxLength} characters.");
        }

        var evaluation = WritingEvaluator.Evaluate(task, text);

        var submission = new WritingSubmission
        {
            UserId = userId,
            WritingTaskId = task.Id,
            Text = text,
            WordCount = evaluation.WordCount,
            ScorePercent = evaluation.ScorePercent,
            Passed = evaluation.Passed,
            FeedbackJson = JsonSerializer.Serialize(evaluation.Checks, JsonSerializerOptions.Web),
            ReviewStatus = WritingReviewStatus.NotRequested
        };

        db.WritingSubmissions.Add(submission);
        await db.SaveChangesAsync(ct);
        return ToDto(submission, task);
    }

    public async Task<WritingSubmissionDto> RequestReviewAsync(Guid userId, Guid submissionId, CancellationToken ct)
    {
        var submission = await db.WritingSubmissions.FirstOrDefaultAsync(s => s.Id == submissionId && s.UserId == userId, ct)
            ?? throw new NotFoundException("submission_not_found", "Submission not found.");

        var task = await db.WritingTasks.AsNoTracking().FirstAsync(t => t.Id == submission.WritingTaskId, ct);

        if (submission.ReviewStatus == WritingReviewStatus.NotRequested)
        {
            submission.ReviewStatus = WritingReviewStatus.Pending;
            await db.SaveChangesAsync(ct);
        }

        return ToDto(submission, task);
    }

    private static WritingSubmissionDto ToDto(WritingSubmission submission, WritingTask task) => new(
        submission.Id,
        submission.Text,
        submission.WordCount,
        submission.ScorePercent,
        submission.Passed,
        ParseChecks(submission.FeedbackJson),
        task.SampleAnswer,
        submission.ReviewStatus,
        submission.TeacherFeedback,
        submission.TeacherScorePercent,
        submission.CreatedAtUtc);

    private static List<WritingCheckDto> ParseChecks(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<WritingCheckDto>>(json, JsonSerializerOptions.Web) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}