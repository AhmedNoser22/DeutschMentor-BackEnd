namespace DeutschMentor.Application.Learning.Dtos;

public enum LevelStatus
{
    Locked,
    InProgress,
    Passed
}

public enum LessonState
{
    Locked,
    Open,
    Current,
    Completed
}

public enum ExamState
{
    Locked,
    Ready,
    Passed
}

public sealed record LevelSummaryDto(
    string Code,
    string Title,
    string Description,
    LevelStatus Status,
    int TotalLessons,
    int CompletedLessons,
    int? PassedScorePercent);

public sealed record LevelRequirementsDto(
    string PreviousLevelCode,
    int PreviousTotalLessons,
    int PreviousCompletedLessons,
    bool PreviousExamPassed);

public sealed record LevelExamDto(ExamState State, int LessonsRemaining, int PassMarkPercent, string? NextLevelCode);

public sealed record LessonSummaryDto(
    Guid Id,
    string Code,
    string Title,
    string TitleEn,
    int EstimatedMinutes,
    LessonState State);

public sealed record UnitDto(Guid Id, int Order, string Title, IReadOnlyList<LessonSummaryDto> Lessons);

public sealed record ModuleDto(
    Guid Id,
    int Order,
    string Title,
    string TitleEn,
    int TotalLessons,
    int CompletedLessons,
    IReadOnlyList<UnitDto> Units);

public sealed record LevelDetailDto(
    LevelSummaryDto Summary,
    LevelRequirementsDto? Requirements,
    LevelExamDto Exam,
    IReadOnlyList<ModuleDto> Modules);

public sealed record LessonDetailDto(
    Guid Id,
    string Code,
    string LevelCode,
    int ModuleOrder,
    int UnitOrder,
    int LessonOrder,
    string Title,
    string TitleEn,
    string TitleAr,
    int EstimatedMinutes,
    IReadOnlyList<LessonStep> StepsSeen,
    bool IsCompleted);

public sealed record LessonProgressDto(IReadOnlyList<LessonStep> StepsSeen, bool IsCompleted, bool CanFinish);

public sealed record FinishLessonResponse(Guid? NextLessonId, bool LevelReadyForExam);