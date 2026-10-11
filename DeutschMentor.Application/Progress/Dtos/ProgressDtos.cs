namespace DeutschMentor.Application.Progress.Dtos;

public enum PlanItemKind
{
    WarmUp,
    Review,
    Lesson,
    Drill
}

public sealed record StreakDto(int Current, int Longest, bool StudiedToday);

public sealed record WeekDayDto(string Date, bool Active, bool IsToday, bool IsFuture);

public sealed record ContinueLessonDto(
    Guid LessonId,
    string Code,
    string LevelCode,
    int ModuleOrder,
    int UnitOrder,
    string Title,
    string TitleEn,
    string TitleAr,
    int StepsDone,
    int TotalSteps,
    int MinutesLeft);

public sealed record PlanItemDto(PlanItemKind Kind, string Title, int Minutes, bool Done, Guid? LessonId);

public sealed record DailyPlanDto(IReadOnlyList<PlanItemDto> Items, int DoneCount, int MinutesLeft);

public sealed record MentorMessageDto(string Title, string Body);

public sealed record AchievementDto(
    string Code,
    string Title,
    string Description,
    string Icon,
    bool Unlocked,
    DateTime? UnlockedAtUtc,
    string? Detail,
    int Current,
    int Target);

public sealed record TodayDto(
    StreakDto Streak,
    IReadOnlyList<WeekDayDto> Week,
    ContinueLessonDto? Continue,
    DailyPlanDto Plan,
    ReviewSummaryDto Review,
    IReadOnlyList<MentorMessageDto> Mentor,
    IReadOnlyList<AchievementDto> RecentAchievements,
    DateTime ServerNowUtc);