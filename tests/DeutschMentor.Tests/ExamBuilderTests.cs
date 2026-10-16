using DeutschMentor.Application.Exams;
using DeutschMentor.Domain.Entities;
using DeutschMentor.Domain.Enums;

namespace DeutschMentor.Tests;

public class ExamBuilderTests
{
    private static List<ExamQuestion> Pool(int perSkill, Guid? levelId = null) =>
        ExamBuilder.SkillOrder
            .SelectMany(skill => Enumerable.Range(0, perSkill).Select(_ => new ExamQuestion { Skill = skill, LevelId = levelId ?? Guid.NewGuid() }))
            .ToList();

    [Fact]
    public void Level_exam_has_the_requested_size_and_is_balanced()
    {
        var result = ExamBuilder.SelectLevelQuestions(Pool(20), 40);

        Assert.Equal(40, result.Count);
        Assert.All(ExamBuilder.SkillOrder, skill => Assert.Equal(10, result.Count(q => q.Skill == skill)));
    }

    [Fact]
    public void Level_exam_is_ordered_by_skill()
    {
        var order = ExamBuilder.SelectLevelQuestions(Pool(20), 40).Select(q => Array.IndexOf(ExamBuilder.SkillOrder, q.Skill)).ToList();
        Assert.Equal(order.Order().ToList(), order);
    }

    [Fact]
    public void Small_pool_returns_everything_without_duplicates()
    {
        var pool = Pool(3);
        var result = ExamBuilder.SelectLevelQuestions(pool, 40);

        Assert.Equal(12, result.Count);
        Assert.Equal(12, result.Distinct().Count());
    }

    [Fact]
    public void Placement_takes_at_most_n_per_level()
    {
        var levels = Enum.GetValues<CefrLevel>().Select(code => new Level { Code = code }).ToList();
        var pool = levels.SelectMany(level => Pool(5, level.Id)).ToList();

        var result = ExamBuilder.SelectPlacementQuestions(levels, pool, 4);

        Assert.Equal(20, result.Count);
        Assert.All(levels, level => Assert.Equal(4, result.Count(q => q.LevelId == level.Id)));
    }
}