namespace DeutschMentor.Application.Exams;

internal static class ExamBuilder
{
    public static readonly ExerciseSkill[] SkillOrder =
    [
        ExerciseSkill.Vocabulary,
        ExerciseSkill.Grammar,
        ExerciseSkill.Reading,
        ExerciseSkill.Writing
    ];

    public static List<ExamQuestion> SelectLevelQuestions(IReadOnlyList<ExamQuestion> pool, int count)
    {
        var perSkill = (int)Math.Ceiling(count / (double)SkillOrder.Length);
        var picked = new List<ExamQuestion>();
        var spare = new List<ExamQuestion>();

        foreach (var skill in SkillOrder)
        {
            var group = pool.Where(q => q.Skill == skill).ToArray();
            Random.Shared.Shuffle(group);
            picked.AddRange(group.Take(perSkill));
            spare.AddRange(group.Skip(perSkill));
        }

        if (picked.Count < count)
        {
            var extra = spare.ToArray();
            Random.Shared.Shuffle(extra);
            picked.AddRange(extra.Take(count - picked.Count));
        }

        return picked
            .Take(count)
            .OrderBy(q => Array.IndexOf(SkillOrder, q.Skill))
            .ToList();
    }

    public static List<ExamQuestion> SelectPlacementQuestions(
        IReadOnlyList<Level> levels,
        IReadOnlyList<ExamQuestion> pool,
        int perLevel)
    {
        var result = new List<ExamQuestion>();

        foreach (var level in levels)
        {
            var group = pool.Where(q => q.LevelId == level.Id).ToArray();
            Random.Shared.Shuffle(group);
            result.AddRange(group.Take(perLevel).OrderBy(q => Array.IndexOf(SkillOrder, q.Skill)));
        }

        return result;
    }
}