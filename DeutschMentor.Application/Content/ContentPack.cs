namespace DeutschMentor.Application.Content;

public sealed class ContentPack
{
    public int SchemaVersion { get; set; }
    public string? Level { get; set; }
    public List<PackModule>? Modules { get; set; }
    public List<PackExam>? Exam { get; set; }
}

public sealed class PackModule { public int Order { get; set; } public string? Title { get; set; } public string? TitleEn { get; set; } public List<PackUnit>? Units { get; set; } }

public sealed class PackUnit { public int Order { get; set; } public string? Title { get; set; } public string? TitleEn { get; set; } public List<PackLesson>? Lessons { get; set; } }

public sealed class PackLesson
{
    public int Order { get; set; }
    public string? Code { get; set; }
    public string? Title { get; set; }
    public string? TitleEn { get; set; }
    public string? TitleAr { get; set; }
    public int Minutes { get; set; }
    public List<PackWord>? Vocabulary { get; set; }
    public List<PackGrammar>? Grammar { get; set; }
    public List<PackExercise>? Exercises { get; set; }
    public List<PackReading>? Reading { get; set; }
    public List<PackWriting>? Writing { get; set; }
}

public sealed class PackWord
{
    public string? German { get; set; }
    public string? Article { get; set; }
    public string? Pos { get; set; }
    public string? English { get; set; }
    public string? Arabic { get; set; }
    public string? Example { get; set; }
    public string? ExampleEn { get; set; }
}

public sealed class PackTable { public List<string>? Headers { get; set; } public List<List<string>>? Rows { get; set; } public List<int>? Highlight { get; set; } }

public sealed class PackGrammar { public string? Title { get; set; } public string? Summary { get; set; } public PackTable? Table { get; set; } public string? NoteEn { get; set; } public string? NoteAr { get; set; } }

public class PackExercise
{
    public string? Type { get; set; }
    public string? Prompt { get; set; }
    public List<string>? Options { get; set; }
    public string? Correct { get; set; }
    public bool? Answer { get; set; }
    public List<List<string>>? Blanks { get; set; }
    public List<string>? Solution { get; set; }
    public List<List<string>>? Pairs { get; set; }
    public string? ExplainEn { get; set; }
    public string? ExplainAr { get; set; }
}

public sealed class PackExam : PackExercise { public string? Skill { get; set; } }

public sealed class PackLine { public string? Speaker { get; set; } public string? Text { get; set; } }

public sealed class PackReading { public string? Title { get; set; } public List<PackLine>? Lines { get; set; } public List<PackExercise>? Questions { get; set; } }

public sealed class PackGroup { public string? Label { get; set; } public List<string>? AnyOf { get; set; } }

public sealed class PackWriting
{
    public string? Title { get; set; }
    public string? Prompt { get; set; }
    public string? Sample { get; set; }
    public int MinWords { get; set; }
    public int Pass { get; set; }
    public List<PackGroup>? Groups { get; set; }
}