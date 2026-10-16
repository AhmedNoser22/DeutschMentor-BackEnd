using DeutschMentor.Application.Content;

namespace DeutschMentor.Tests;

public class ContentValidatorTests
{
    private const string Ar = "\u0639\u0631\u0628\u064a";

    private static PackExercise Choice() => new()
    {
        Type = "MultipleChoice",
        Prompt = "Q?",
        Options = ["a", "b"],
        Correct = "a",
        ExplainEn = "e",
        ExplainAr = Ar
    };

    private static PackWriting Writing(string sample = "Guten Morgen! Tschüss!") => new()
    {
        Title = "t",
        Prompt = "p",
        Sample = sample,
        MinWords = 3,
        Pass = 60,
        Groups = [new PackGroup { Label = "g", AnyOf = ["guten morgen", "hallo"] }, new PackGroup { Label = "b", AnyOf = ["tschüss"] }]
    };

    private static ContentPack Pack(Action<PackLesson>? change = null, string code = "A1-1-01")
    {
        var lesson = new PackLesson
        {
            Order = 1,
            Code = code,
            Title = "T",
            TitleEn = "T",
            TitleAr = Ar,
            Minutes = 10,
            Vocabulary = [new PackWord { German = "Tisch", Article = "der", Pos = "Noun", English = "table", Arabic = Ar, Example = "Der Tisch ist groß.", ExampleEn = "The table is big." }],
            Exercises = [Choice()],
            Writing = [Writing()]
        };
        change?.Invoke(lesson);

        return new ContentPack
        {
            SchemaVersion = 1,
            Level = "A1",
            Modules = [new PackModule { Order = 1, Title = "M", TitleEn = "M", Units = [new PackUnit { Order = 1, Title = "U", TitleEn = "U", Lessons = [lesson] }] }]
        };
    }

    private static void AssertError(ImportReport report, string text) =>
        Assert.Contains(report.Errors, e => e.Contains(text, StringComparison.OrdinalIgnoreCase));

    [Fact]
    public void A_correct_pack_has_no_errors() => Assert.Empty(ContentValidator.Validate(Pack()).Errors);

    [Fact]
    public void Noun_without_article_is_rejected() =>
        AssertError(ContentValidator.Validate(Pack(l => l.Vocabulary![0].Article = null)), "needs an article");

    [Fact]
    public void Lowercase_noun_is_rejected() =>
        AssertError(ContentValidator.Validate(Pack(l => l.Vocabulary![0].German = "tisch")), "capital letter");

    [Fact]
    public void Missing_arabic_is_rejected() =>
        AssertError(ContentValidator.Validate(Pack(l => l.Vocabulary![0].Arabic = "table")), "arabic must contain");

    [Fact]
    public void Correct_answer_must_be_one_of_the_options() =>
        AssertError(ContentValidator.Validate(Pack(l => l.Exercises![0].Correct = "z")), "correct must match");

    [Fact]
    public void Blank_count_must_match_the_prompt() =>
        AssertError(
            ContentValidator.Validate(Pack(l => l.Exercises = [new PackExercise { Type = "FillBlank", Prompt = "A ___ B ___", Blanks = [["x"]], ExplainEn = "e", ExplainAr = Ar }])),
            "number of ___");

    [Fact]
    public void Sample_answer_must_pass_its_own_rules() =>
        AssertError(ContentValidator.Validate(Pack(l => l.Writing = [Writing("Hallo")])), "sample answer fails");

    [Fact]
    public void Module_number_in_the_code_must_match() =>
        AssertError(ContentValidator.Validate(Pack(code: "A1-2-01")), "module number");

    [Fact]
    public void Wrong_level_prefix_is_rejected() =>
        AssertError(ContentValidator.Validate(Pack(code: "B1-1-01")), "code must look like");
}