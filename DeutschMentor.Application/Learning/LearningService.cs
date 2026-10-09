namespace DeutschMentor.Application.Learning;

public sealed class LearningService(IApplicationDbContext db, TimeProvider clock) : ILearningService
{
    public async Task<IReadOnlyList<LevelSummaryDto>> GetLevelsAsync(Guid userId, CancellationToken ct)
    {
        var path = await LoadPathAsync(userId, ct);
        return path.Levels.Select(ToSummary).ToList();
    }

    public async Task<LevelDetailDto> GetLevelAsync(Guid userId, string code, CancellationToken ct)
    {
        if (!Enum.TryParse<CefrLevel>(code, true, out var cefr) || !Enum.IsDefined(cefr))
        {
            throw new NotFoundException("level_not_found", "Level not found.");
        }

        var path = await LoadPathAsync(userId, ct);
        var node = path.Levels.FirstOrDefault(l => l.Level.Code == cefr)
            ?? throw new NotFoundException("level_not_found", "Level not found.");

        var previous = path.Levels.FirstOrDefault(l => l.Level.Order == node.Level.Order - 1);
        var next = path.Levels.FirstOrDefault(l => l.Level.Order == node.Level.Order + 1);

        var requirements = node.Unlocked || previous is null
            ? null
            : new LevelRequirementsDto(
                previous.Level.Code.ToString(),
                previous.Lessons.Count,
                previous.CompletedCount,
                previous.Passed);

        var exam = new LevelExamDto(
            ExamStateOf(node),
            node.Lessons.Count - node.CompletedCount,
            node.Level.PassMarkPercent,
            next?.Level.Code.ToString());

        var modules = node.Unlocked ? BuildModules(node, path.States) : [];
        return new LevelDetailDto(ToSummary(node), requirements, exam, modules);
    }

    public async Task<LessonDetailDto> GetLessonAsync(Guid userId, Guid lessonId, CancellationToken ct)
    {
        var (path, row) = await RequireAccessAsync(userId, lessonId, ct);
        var level = path.Levels.First(l => l.Level.Id == row.LevelId).Level;

        var progress = await db.LessonProgress.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.LessonId == lessonId, ct);

        return new LessonDetailDto(
            row.Id,
            row.Code,
            level.Code.ToString(),
            row.ModuleOrder,
            row.UnitOrder,
            row.LessonOrder,
            row.Title,
            row.TitleEn,
            row.TitleAr,
            row.EstimatedMinutes,
            progress?.SeenSteps ?? [],
            progress?.CompletedAtUtc is not null);
    }

    public async Task<LessonProgressDto> MarkStepSeenAsync(Guid userId, Guid lessonId, LessonStep step, CancellationToken ct)
    {
        if (!Enum.IsDefined(step))
        {
            throw new BadRequestException("invalid_step", "Unknown lesson step.");
        }

        await RequireAccessAsync(userId, lessonId, ct);

        var progress = await db.LessonProgress
            .FirstOrDefaultAsync(p => p.UserId == userId && p.LessonId == lessonId, ct);

        if (progress is null)
        {
            progress = new UserLessonProgress { UserId = userId, LessonId = lessonId };
            db.LessonProgress.Add(progress);
        }

        progress.MarkSeen(step);
        await db.SaveChangesAsync(ct);
        return ToProgressDto(progress);
    }

    public async Task<FinishLessonResponse> FinishLessonAsync(Guid userId, Guid lessonId, CancellationToken ct)
    {
        var (path, row) = await RequireAccessAsync(userId, lessonId, ct);
        var incomplete = new BadRequestException("steps_incomplete", "Open every step before finishing the lesson.");

        var progress = await db.LessonProgress
            .FirstOrDefaultAsync(p => p.UserId == userId && p.LessonId == lessonId, ct);

        if (progress is null || !progress.AllStepsSeen)
        {
            throw incomplete;
        }

        var firstTime = progress.CompletedAtUtc is null;
        var wasCurrent = path.States[lessonId] == LessonState.Current;
        progress.CompletedAtUtc ??= clock.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);

        var node = path.Levels.First(l => l.Level.Id == row.LevelId);
        var index = node.Lessons.FindIndex(l => l.Id == lessonId);
        var next = node.Lessons.Skip(index + 1).FirstOrDefault();

        var nextAccessible = next is not null
            && (path.States[next.Id] != LessonState.Locked || (firstTime && wasCurrent));

        var completedNow = node.CompletedCount + (firstTime ? 1 : 0);
        return new FinishLessonResponse(nextAccessible ? next!.Id : null, completedNow == node.Lessons.Count);
    }

    public async Task EnsureLessonAccessAsync(Guid userId, Guid lessonId, CancellationToken ct) =>
        await RequireAccessAsync(userId, lessonId, ct);

    public async Task<IReadOnlySet<Guid>> GetAccessibleLessonIdsAsync(Guid userId, CancellationToken ct)
    {
        var path = await LoadPathAsync(userId, ct);
        return path.States.Where(s => s.Value != LessonState.Locked).Select(s => s.Key).ToHashSet();
    }

    private async Task<(PathSnapshot Path, LessonRow Row)> RequireAccessAsync(Guid userId, Guid lessonId, CancellationToken ct)
    {
        var path = await LoadPathAsync(userId, ct);

        if (!path.Rows.TryGetValue(lessonId, out var row))
        {
            throw new NotFoundException("lesson_not_found", "Lesson not found.");
        }

        if (path.States[lessonId] == LessonState.Locked)
        {
            throw new ForbiddenException("lesson_locked", "Finish the previous lessons first.");
        }

        return (path, row);
    }

    private async Task<PathSnapshot> LoadPathAsync(Guid userId, CancellationToken ct)
    {
        var levels = await db.Levels.AsNoTracking().OrderBy(l => l.Order).ToListAsync(ct);

        var rows = await db.Lessons.AsNoTracking()
            .Where(l => l.Status == ContentStatus.Published)
            .Select(l => new LessonRow(
                l.Id,
                l.Code,
                l.Title,
                l.TitleEn,
                l.TitleAr,
                l.EstimatedMinutes,
                l.Order,
                l.UnitId,
                l.Unit!.Order,
                l.Unit.Title,
                l.Unit.ModuleId,
                l.Unit.Module!.Order,
                l.Unit.Module.Title,
                l.Unit.Module.TitleEn,
                l.Unit.Module.LevelId))
            .ToListAsync(ct);

        var completed = (await db.LessonProgress.AsNoTracking()
            .Where(p => p.UserId == userId && p.CompletedAtUtc != null)
            .Select(p => p.LessonId)
            .ToListAsync(ct)).ToHashSet();

        var passes = await db.LevelPasses.AsNoTracking()
            .Where(p => p.UserId == userId)
            .ToDictionaryAsync(p => p.LevelId, p => p.ScorePercent, ct);

        var nodes = new List<LevelNode>();
        foreach (var level in levels)
        {
            var lessons = rows
                .Where(r => r.LevelId == level.Id)
                .OrderBy(r => r.ModuleOrder)
                .ThenBy(r => r.UnitOrder)
                .ThenBy(r => r.LessonOrder)
                .ToList();

            var passed = passes.TryGetValue(level.Id, out var score);
            var unlocked = nodes.Count == 0 || nodes[^1].Passed;
            var completedCount = lessons.Count(l => completed.Contains(l.Id));

            nodes.Add(new LevelNode(level, lessons, unlocked, passed, passed ? score : null, completedCount));
        }

        var states = new Dictionary<Guid, LessonState>();
        foreach (var node in nodes)
        {
            var currentAssigned = false;
            foreach (var lesson in node.Lessons)
            {
                if (completed.Contains(lesson.Id))
                {
                    states[lesson.Id] = LessonState.Completed;
                }
                else if (!node.Unlocked)
                {
                    states[lesson.Id] = LessonState.Locked;
                }
                else if (node.Passed)
                {
                    states[lesson.Id] = LessonState.Open;
                }
                else if (!currentAssigned)
                {
                    states[lesson.Id] = LessonState.Current;
                    currentAssigned = true;
                }
                else
                {
                    states[lesson.Id] = LessonState.Locked;
                }
            }
        }

        return new PathSnapshot(nodes, states, rows.ToDictionary(r => r.Id));
    }

    private static LevelSummaryDto ToSummary(LevelNode node) => new(
        node.Level.Code.ToString(),
        node.Level.Title,
        node.Level.Description,
        !node.Unlocked ? LevelStatus.Locked : node.Passed ? LevelStatus.Passed : LevelStatus.InProgress,
        node.Lessons.Count,
        node.CompletedCount,
        node.Score);

    private static ExamState ExamStateOf(LevelNode node)
    {
        if (node.Passed)
        {
            return ExamState.Passed;
        }

        var ready = node.Unlocked && node.Lessons.Count > 0 && node.CompletedCount == node.Lessons.Count;
        return ready ? ExamState.Ready : ExamState.Locked;
    }

    private static LessonProgressDto ToProgressDto(UserLessonProgress progress) =>
        new(progress.SeenSteps, progress.CompletedAtUtc is not null, progress.AllStepsSeen);

    private static List<ModuleDto> BuildModules(LevelNode level, IReadOnlyDictionary<Guid, LessonState> states) =>
        level.Lessons
            .GroupBy(l => l.ModuleId)
            .Select(module =>
            {
                var first = module.First();

                var units = module
                    .GroupBy(l => l.UnitId)
                    .Select(unit =>
                    {
                        var head = unit.First();
                        var lessons = unit
                            .OrderBy(l => l.LessonOrder)
                            .Select(l => new LessonSummaryDto(l.Id, l.Code, l.Title, l.TitleEn, l.EstimatedMinutes, states[l.Id]))
                            .ToList();
                        return new UnitDto(head.UnitId, head.UnitOrder, head.UnitTitle, lessons);
                    })
                    .OrderBy(u => u.Order)
                    .ToList();

                return new ModuleDto(
                    first.ModuleId,
                    first.ModuleOrder,
                    first.ModuleTitle,
                    first.ModuleTitleEn,
                    module.Count(),
                    module.Count(l => states[l.Id] == LessonState.Completed),
                    units);
            })
            .OrderBy(m => m.Order)
            .ToList();
}