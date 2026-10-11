namespace DeutschMentor.Application.Exams.Dtos;

public enum ExamAvailability
{
    Passed,
    Ready,
    InProgress,
    Cooldown,
    LessonsRemaining,
    LevelLocked,
    NotPublished
}

public sealed record ExamLevelDto(
    string Code,
    string Title,
    ExamAvailability Availability,
    int LessonsRemaining,
    string? OpensAfterLevelCode,
    int PassMarkPercent,
    int? PassedScorePercent,
    DateTime? PassedAtUtc,
    DateTime? RetakeAvailableAtUtc,
    Guid? InProgressAttemptId,
    Guid? CertificateId,
    bool PassedByPlacement);

public sealed record PlacementInfoDto(bool Available, Guid? InProgressAttemptId, int QuestionCount, int Minutes);

public sealed record ExamOverviewDto(
    IReadOnlyList<ExamLevelDto> Levels,
    PlacementInfoDto Placement,
    int PassMarkPercent,
    int RetakeCooldownHours,
    DateTime ServerNowUtc);

public sealed record ExamQuestionDto(ExerciseDto Exercise, ExerciseSkill Skill);

public sealed record SavedAnswerDto(Guid QuestionId, IReadOnlyList<string> Answer, bool Flagged);

public sealed record ExamSessionDto(
    Guid AttemptId,
    ExamKind Kind,
    string? LevelCode,
    string Title,
    ExamAttemptStatus Status,
    DateTime StartedAtUtc,
    DateTime ExpiresAtUtc,
    DateTime ServerNowUtc,
    IReadOnlyList<ExamQuestionDto> Questions,
    IReadOnlyList<SavedAnswerDto> Answers);

public sealed record SaveAnswerRequest(List<string>? Answer, bool Flagged);

public sealed record ExamAnswerInput(Guid QuestionId, List<string>? Answer, bool Flagged);

public sealed record SubmitExamRequest(List<ExamAnswerInput>? Answers);

public sealed record ScoreBreakdownDto(string Label, int Correct, int Total, int Percent);

public sealed record ExamResultDto(
    Guid AttemptId,
    ExamKind Kind,
    string? LevelCode,
    string Title,
    ExamAttemptStatus Status,
    int ScorePercent,
    int CorrectCount,
    int TotalQuestions,
    int PassMarkPercent,
    bool Passed,
    IReadOnlyList<ScoreBreakdownDto> Breakdown,
    IReadOnlyList<string> WeakAreas,
    DateTime SubmittedAtUtc,
    DateTime? RetakeAvailableAtUtc,
    DateTime ServerNowUtc,
    Guid? CertificateId,
    string? NextLevelCode,
    string? PlacedLevelCode);