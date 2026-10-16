using DeutschMentor.Application.Writing;
using DeutschMentor.Domain.Entities;

namespace DeutschMentor.Tests;

public class WritingEvaluatorTests
{
    private static readonly WritingTask Task = new()
    {
        MinWords = 4,
        PassPercent = 60,
        RulesJson = """{"groups":[{"label":"greeting","anyOf":["guten morgen","hallo"]},{"label":"goodbye","anyOf":["tschüss"]}]}"""
    };

    [Fact]
    public void Full_answer_scores_one_hundred()
    {
        var result = WritingEvaluator.Evaluate(Task, "Guten Morgen! Tschüss, bis morgen!");
        Assert.Equal(100, result.ScorePercent);
        Assert.True(result.Passed);
    }

    [Fact]
    public void Missing_group_and_short_text_fails()
    {
        var result = WritingEvaluator.Evaluate(Task, "Hallo");
        Assert.Equal(45, result.ScorePercent);
        Assert.False(result.Passed);
    }

    [Fact]
    public void Both_groups_but_short_text_still_passes()
    {
        var result = WritingEvaluator.Evaluate(Task, "Hallo Tschüss");
        Assert.Equal(90, result.ScorePercent);
        Assert.True(result.Passed);
    }

    [Fact]
    public void Matching_ignores_case_and_punctuation() =>
        Assert.Contains(WritingEvaluator.Evaluate(Task, "GUTEN MORGEN!!!").Checks, c => c.Label == "greeting" && c.Met);

    [Fact]
    public void Matching_respects_word_boundaries() =>
        Assert.Contains(WritingEvaluator.Evaluate(Task, "Tschüssen").Checks, c => c.Label == "goodbye" && !c.Met);
}