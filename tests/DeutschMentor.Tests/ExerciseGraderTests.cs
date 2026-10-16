using DeutschMentor.Application.Exercises;
using DeutschMentor.Domain.Enums;

namespace DeutschMentor.Tests;

public class ExerciseGraderTests
{
    [Theory]
    [InlineData("b", true)]
    [InlineData("a", false)]
    public void Multiple_choice(string answer, bool expected)
    {
        const string payload = """{"options":["a","b"],"correctIndex":1}""";
        Assert.Equal(expected, ExerciseGrader.Grade(ExerciseType.MultipleChoice, payload, [answer]).IsCorrect);
    }

    [Fact]
    public void True_false_ignores_case() =>
        Assert.True(ExerciseGrader.Grade(ExerciseType.TrueFalse, """{"correct":true}""", ["TRUE"]).IsCorrect);

    [Theory]
    [InlineData("möchten", true)]
    [InlineData("  Möchte ", true)]
    [InlineData("möchtet", false)]
    public void Fill_blank_accepts_variants_and_ignores_case_and_spaces(string answer, bool expected)
    {
        const string payload = """{"blanks":[["möchte","möchten"]]}""";
        Assert.Equal(expected, ExerciseGrader.Grade(ExerciseType.FillBlank, payload, [answer]).IsCorrect);
    }

    [Fact]
    public void Ordering_needs_the_exact_sequence()
    {
        const string payload = """{"solution":["Ich","komme","aus","Ägypten."]}""";
        Assert.True(ExerciseGrader.Grade(ExerciseType.Ordering, payload, ["Ich", "komme", "aus", "Ägypten."]).IsCorrect);
        Assert.False(ExerciseGrader.Grade(ExerciseType.Ordering, payload, ["komme", "Ich", "aus", "Ägypten."]).IsCorrect);
    }

    [Fact]
    public void Matching_accepts_any_order_but_not_wrong_pairs()
    {
        const string payload = """{"pairs":[{"left":"a","right":"1"},{"left":"b","right":"2"}]}""";
        Assert.True(ExerciseGrader.Grade(ExerciseType.Matching, payload, ["b::2", "a::1"]).IsCorrect);
        Assert.False(ExerciseGrader.Grade(ExerciseType.Matching, payload, ["a::2", "b::1"]).IsCorrect);
        Assert.False(ExerciseGrader.Grade(ExerciseType.Matching, payload, ["a::1"]).IsCorrect);
    }

    [Fact]
    public void Ordering_dto_contains_the_same_words_shuffled()
    {
        const string payload = """{"solution":["Ich","komme","aus","Ägypten."]}""";
        var dto = ExerciseGrader.ToDto(Guid.NewGuid(), 1, ExerciseType.Ordering, "Order", payload);
        Assert.Equal(new[] { "Ich", "komme", "aus", "Ägypten." }.Order(), dto.Words.Order());
    }

    [Fact]
    public void Multiple_choice_dto_does_not_expose_the_answer()
    {
        const string payload = """{"options":["a","b"],"correctIndex":1}""";
        var dto = ExerciseGrader.ToDto(Guid.NewGuid(), 1, ExerciseType.MultipleChoice, "Q", payload);
        Assert.Equal(["a", "b"], dto.Options);
        Assert.DoesNotContain(dto.GetType().GetProperties(), p => p.Name.Contains("Correct", StringComparison.OrdinalIgnoreCase));
    }
}