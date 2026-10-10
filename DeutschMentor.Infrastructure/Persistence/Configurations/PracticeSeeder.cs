namespace DeutschMentor.Infrastructure.Persistence;

public static class PracticeSeeder
{
    private sealed record Line(string? Speaker, string Text);

    private sealed record Group(string Label, string[] AnyOf);

    public static async Task SeedAsync(ApplicationDbContext db)
    {
        if (await db.Exercises.AnyAsync() || await db.WritingTasks.AnyAsync())
        {
            return;
        }

        var hello = await db.Lessons.FirstOrDefaultAsync(l => l.Code == "A1-1-01");
        var drinks = await db.Lessons.FirstOrDefaultAsync(l => l.Code == "A2-1-02");
        if (hello is null || drinks is null)
        {
            return;
        }

        var helloReading = new ReadingPassage
        {
            LessonId = hello.Id,
            Order = 1,
            Title = "Guten Morgen",
            BodyJson = Json(new[]
            {
                new Line("Anna", "Guten Morgen, Tom!"),
                new Line("Tom", "Guten Morgen, Anna! Wie geht's?"),
                new Line("Anna", "Gut, danke."),
                new Line("Tom", "Tschüss, bis morgen!")
            })
        };

        var drinksReading = new ReadingPassage
        {
            LessonId = drinks.Id,
            Order = 1,
            Title = "Im Café",
            BodyJson = Json(new[]
            {
                new Line("Kellner", "Guten Tag! Was möchten Sie trinken?"),
                new Line("Lina", "Ich möchte einen Tee, bitte."),
                new Line("Kellner", "Und für Sie?"),
                new Line("Karim", "Ein Wasser und eine Apfelschorle, bitte."),
                new Line("Kellner", "Kommt sofort.")
            })
        };

        db.ReadingPassages.AddRange(helloReading, drinksReading);

        db.Exercises.AddRange(
            Choice(hello, null, 1, "Which greeting do you use in the morning?",
                ["Gute Nacht", "Guten Morgen", "Guten Abend", "Tschüss"], 1,
                "Guten Morgen means good morning.",
                "«Guten Morgen» تعني صباح الخير."),
            Fill(hello, null, 2, "___ Abend, Frau Meier.", [["Guten"]],
                "Greetings use guten: Guten Abend, Guten Tag, Guten Morgen.",
                "التحيات تستخدم «Guten»: Guten Abend وGuten Tag وGuten Morgen."),
            Match(hello, null, 3, "Match the German word with its meaning.",
                [("Hallo", "hello"), ("Tschüss", "bye"), ("Danke", "thank you")],
                "Hallo is hello, Tschüss is bye, and Danke is thank you.",
                "Hallo تعني مرحبًا، وTschüss تعني مع السلامة، وDanke تعني شكرًا."),

            TrueFalse(hello, helloReading.Id, 1, "It is the morning.", true,
                "They say Guten Morgen, so it is the morning.",
                "يقولان «Guten Morgen» إذًا الوقت صباح."),
            TrueFalse(hello, helloReading.Id, 2, "Anna says goodbye.", false,
                "Tom says Tschüss. Anna does not.",
                "توم هو الذي يقول «Tschüss» وليست آنا."),

            Match(drinks, null, 1, "Match the German word with its meaning.",
                [("Wasser", "water"), ("Kaffee", "coffee"), ("Milch", "milk"), ("bestellen", "to order")],
                "These are the four words from this lesson.",
                "هذه هي الكلمات الأربع من هذا الدرس."),
            Choice(drinks, null, 2, "Ich möchte ___ Kaffee, bitte.",
                ["ein", "einen", "eine", "einem"], 1,
                "Kaffee is a der-word, and the thing you order takes the accusative: einen Kaffee.",
                "القهوة مذكّر (der)، والمفعول به يأخذ الحالة Akkusativ، لذلك نقول einen."),
            Fill(drinks, null, 3, "Ich ___ ein Wasser, bitte.", [["möchte"]],
                "With ich, möchten becomes möchte.",
                "مع «ich» يصبح الفعل «möchte»."),
            Order(drinks, null, 4, "Put the words in the right order.",
                ["Wir", "bestellen", "zwei", "Tassen", "Tee."],
                "The verb takes the second position: Wir bestellen zwei Tassen Tee.",
                "الفعل يأتي في المركز الثاني من الجملة."),
            TrueFalse(drinks, null, 5, "The word Milch takes the article \"die\".", true,
                "It is die Milch.",
                "نقول «die Milch»."),
            Choice(drinks, null, 6, "Möchten Sie ___ Wasser?",
                ["ein", "einen", "eine", "einem"], 0,
                "Wasser is a das-word. Neuter nouns keep ein in the accusative.",
                "الماء محايد (das)، والمحايد يبقى «ein» في Akkusativ."),

            TrueFalse(drinks, drinksReading.Id, 1, "Lina orders a coffee.", false,
                "Lina says: Ich möchte einen Tee. She orders tea.",
                "لينا تقول «Ich möchte einen Tee»، أي أنها تطلب شايًا."),
            TrueFalse(drinks, drinksReading.Id, 2, "Karim orders two drinks.", true,
                "He orders a water and an apple spritzer.",
                "يطلب كريم ماءً وعصير تفاح مع ماء غازي."));

        db.WritingTasks.AddRange(
            new WritingTask
            {
                LessonId = hello.Id,
                Order = 1,
                Title = "Say hello",
                Prompt = "Write a short German greeting for the morning, then say goodbye.",
                SampleAnswer = "Guten Morgen! Wie geht's? Tschüss, bis morgen!",
                MinWords = 4,
                PassPercent = 60,
                RulesJson = Json(new
                {
                    groups = new[]
                    {
                        new Group("A morning greeting (Guten Morgen or Hallo)", ["guten morgen", "hallo"]),
                        new Group("A goodbye (Tschüss or Auf Wiedersehen)", ["tschüss", "auf wiedersehen"])
                    }
                })
            },
            new WritingTask
            {
                LessonId = drinks.Id,
                Order = 1,
                Title = "At the café",
                Prompt = "You are in a café. Write 2 to 3 German sentences: greet the waiter and order a drink.",
                SampleAnswer = "Guten Tag! Ich möchte einen Kaffee und ein Wasser, bitte.",
                MinWords = 8,
                PassPercent = 60,
                RulesJson = Json(new
                {
                    groups = new[]
                    {
                        new Group("Greets the waiter (Guten Tag, Hallo...)", ["guten tag", "hallo", "guten morgen", "guten abend"]),
                        new Group("Uses möchte or möchten", ["möchte", "möchten"]),
                        new Group("Names a drink", ["wasser", "kaffee", "tee", "milch", "saft", "cola"]),
                        new Group("Says bitte", ["bitte"])
                    }
                })
            });

        await db.SaveChangesAsync();
    }

    private static string Json(object value) => JsonSerializer.Serialize(value, JsonSerializerOptions.Web);

    private static Exercise Choice(
        Lesson lesson, Guid? passageId, int order, string prompt, string[] options, int correctIndex, string en, string ar) =>
        Make(lesson, passageId, order, ExerciseType.MultipleChoice, prompt, new { options, correctIndex }, en, ar);

    private static Exercise TrueFalse(
        Lesson lesson, Guid? passageId, int order, string prompt, bool correct, string en, string ar) =>
        Make(lesson, passageId, order, ExerciseType.TrueFalse, prompt, new { correct }, en, ar);

    private static Exercise Fill(
        Lesson lesson, Guid? passageId, int order, string prompt, string[][] blanks, string en, string ar) =>
        Make(lesson, passageId, order, ExerciseType.FillBlank, prompt, new { blanks }, en, ar);

    private static Exercise Order(
        Lesson lesson, Guid? passageId, int order, string prompt, string[] solution, string en, string ar) =>
        Make(lesson, passageId, order, ExerciseType.Ordering, prompt, new { solution }, en, ar);

    private static Exercise Match(
        Lesson lesson, Guid? passageId, int order, string prompt, (string Left, string Right)[] pairs, string en, string ar) =>
        Make(
            lesson, passageId, order, ExerciseType.Matching, prompt,
            new { pairs = pairs.Select(p => new { left = p.Left, right = p.Right }).ToArray() }, en, ar);

    private static Exercise Make(
        Lesson lesson, Guid? passageId, int order, ExerciseType type, string prompt, object payload, string en, string ar) =>
        new()
        {
            LessonId = lesson.Id,
            ReadingPassageId = passageId,
            Order = order,
            Type = type,
            Prompt = prompt,
            PayloadJson = Json(payload),
            ExplanationEn = en,
            ExplanationAr = ar
        };
}