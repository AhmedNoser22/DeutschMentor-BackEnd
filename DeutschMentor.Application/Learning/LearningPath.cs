namespace DeutschMentor.Application.Learning;

internal sealed record LessonRow(
    Guid Id,
    string Code,
    string Title,
    string TitleEn,
    string TitleAr,
    int EstimatedMinutes,
    int LessonOrder,
    Guid UnitId,
    int UnitOrder,
    string UnitTitle,
    Guid ModuleId,
    int ModuleOrder,
    string ModuleTitle,
    string ModuleTitleEn,
    Guid LevelId);

internal sealed record LevelNode(
    Level Level,
    List<LessonRow> Lessons,
    bool Unlocked,
    bool Passed,
    int? Score,
    int CompletedCount);

internal sealed record PathSnapshot(
    IReadOnlyList<LevelNode> Levels,
    IReadOnlyDictionary<Guid, LessonState> States,
    IReadOnlyDictionary<Guid, LessonRow> Rows);