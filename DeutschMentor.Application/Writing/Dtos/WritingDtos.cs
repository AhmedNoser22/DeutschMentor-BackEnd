namespace DeutschMentor.Application.Writing.Dtos;

public sealed record WritingCheckDto(string Label, bool Met);

public sealed record WritingSubmissionDto(
    Guid Id,
    string Text,
    int WordCount,
    int ScorePercent,
    bool Passed,
    IReadOnlyList<WritingCheckDto> Checks,
    string SampleAnswer,
    WritingReviewStatus ReviewStatus,
    string? TeacherFeedback,
    int? TeacherScorePercent,
    DateTime SubmittedAtUtc);

public sealed record WritingTaskDto(
    Guid Id,
    int Order,
    string Title,
    string Prompt,
    int MinWords,
    int PassPercent,
    WritingSubmissionDto? Latest);

public sealed record SubmitWritingRequest(string? Text);