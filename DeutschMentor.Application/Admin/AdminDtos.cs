namespace DeutschMentor.Application.Admin;

public sealed record AdminStatsDto(int Learners, int Lessons, int Drafts, int ExamsTaken);

public sealed record AdminLessonDto(
    Guid Id, string Code, string Title, string Level, ContentStatus Status, DateTime CreatedAtUtc,
    int Words, int Grammar, int Exercises, int Reading, int Writing);

public sealed record AdminWritingDto(
    Guid Id, string StudentName, string TaskTitle, string Prompt, string Text, int ScorePercent,
    string SampleAnswer, DateTime SubmittedAtUtc);

public sealed record ReviewWritingRequest(int? ScorePercent, string? Feedback);

public sealed record ExamPoolDto(string Level, string Skill, int Count);