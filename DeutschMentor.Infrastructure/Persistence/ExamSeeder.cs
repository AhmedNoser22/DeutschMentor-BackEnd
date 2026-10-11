namespace DeutschMentor.Infrastructure.Persistence;

public static class ExamSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db)
    {
        if (await db.ExamQuestions.AnyAsync())
        {
            return;
        }

        var a1 = await db.Levels.FirstOrDefaultAsync(l => l.Code == CefrLevel.A1);
        var a2 = await db.Levels.FirstOrDefaultAsync(l => l.Code == CefrLevel.A2);
        if (a1 is null || a2 is null)
        {
            return;
        }

        db.ExamQuestions.AddRange(
            Choice(a1, ExerciseSkill.Vocabulary, "What does \"Danke\" mean?",
                ["please", "thank you", "sorry", "goodbye"], 1,
                "Danke means thank you.",
                "«Danke» تعني شكرًا."),
            Match(a1, ExerciseSkill.Vocabulary, "Match the German word with its meaning.",
                [("Hallo", "hello"), ("Tschüss", "bye"), ("Guten Morgen", "good morning")],
                "Hallo is hello, Tschüss is bye, and Guten Morgen is good morning.",
                "Hallo تعني مرحبًا، وTschüss تعني مع السلامة، وGuten Morgen تعني صباح الخير."),
            Fill(a1, ExerciseSkill.Grammar, "Ich ___ Omar.", [["bin"]],
                "With ich, the verb sein becomes bin.",
                "مع «ich» يصبح الفعل «sein» هكذا: «bin»."),
            Choice(a1, ExerciseSkill.Grammar, "Wie ___ du?",
                ["heiße", "heißt", "heißen", "heiß"], 1,
                "With du, the verb heißen becomes heißt.",
                "مع «du» يصبح الفعل «heißt»."),
            Choice(a1, ExerciseSkill.Reading, "„Ich heiße Lina und ich komme aus Berlin.“ Where does Lina come from?",
                ["Berlin", "Wien", "Zürich", "Hamburg"], 0,
                "Lina says: ich komme aus Berlin.",
                "تقول لينا: «ich komme aus Berlin»."),
            TrueFalse(a1, ExerciseSkill.Reading, "„Guten Abend, Herr Schmidt!“ Someone says hello in the evening.", true,
                "Guten Abend means good evening.",
                "«Guten Abend» تعني مساء الخير."),
            Order(a1, ExerciseSkill.Writing, "Put the words in the right order.",
                ["Ich", "komme", "aus", "Ägypten."],
                "The verb takes the second position.",
                "الفعل يأتي في المركز الثاني من الجملة."),
            Fill(a1, ExerciseSkill.Writing, "Mein Name ___ Anna.", [["ist"]],
                "Mein Name goes with ist.",
                "«Mein Name» يأتي مع «ist»."),

            Choice(a2, ExerciseSkill.Vocabulary, "Which word means \"bill\"?",
                ["Speisekarte", "Rechnung", "Trinkgeld", "Getränk"], 1,
                "Rechnung is the bill. Speisekarte is the menu.",
                "«Rechnung» تعني الفاتورة، و«Speisekarte» تعني قائمة الطعام."),
            Match(a2, ExerciseSkill.Vocabulary, "Match the German word with its meaning.",
                [("Wasser", "water"), ("Milch", "milk"), ("bezahlen", "to pay")],
                "Wasser is water, Milch is milk, and bezahlen is to pay.",
                "Wasser تعني الماء، وMilch تعني الحليب، وbezahlen تعني يدفع."),
            Choice(a2, ExerciseSkill.Grammar, "Ich möchte ___ Kaffee.",
                ["ein", "einen", "eine", "einem"], 1,
                "Kaffee is a der-word, and the thing you order takes the accusative: einen Kaffee.",
                "القهوة مذكّر (der)، والمفعول به يأخذ الحالة Akkusativ، لذلك نقول einen."),
            Fill(a2, ExerciseSkill.Grammar, "Wir ___ zwei Tassen Tee.", [["möchten", "bestellen"]],
                "With wir, the verb keeps its infinitive form: möchten.",
                "مع «wir» يبقى الفعل على صيغة المصدر: «möchten»."),
            Choice(a2, ExerciseSkill.Reading, "„Der Zug fährt um 14:30 ab.“ Wann fährt der Zug?",
                ["Um halb drei", "Um halb vier", "Um halb zwei", "Um zwei"], 0,
                "14:30 is half past two. In German you say halb drei.",
                "الساعة 14:30 تُقال «halb drei» أي الثالثة إلا نصف."),
            TrueFalse(a2, ExerciseSkill.Reading, "„Die Rechnung, bitte!“ The guest wants to pay.", true,
                "Die Rechnung, bitte means the bill, please.",
                "«Die Rechnung, bitte» تعني: الفاتورة من فضلك."),
            Order(a2, ExerciseSkill.Writing, "Put the words in the right order.",
                ["Ich", "möchte", "einen", "Kaffee,", "bitte."],
                "The verb takes the second position.",
                "الفعل يأتي في المركز الثاني من الجملة."),
            Fill(a2, ExerciseSkill.Writing, "Möchten Sie ___ Wasser?", [["ein"]],
                "Wasser is a das-word. Neuter nouns keep ein in the accusative.",
                "الماء محايد (das)، والمحايد يبقى «ein» في Akkusativ."));

        await db.SaveChangesAsync();
    }

    private static string Json(object value) => JsonSerializer.Serialize(value, JsonSerializerOptions.Web);

    private static ExamQuestion Choice(
        Level level, ExerciseSkill skill, string prompt, string[] options, int correctIndex, string en, string ar) =>
        Make(level, skill, ExerciseType.MultipleChoice, prompt, new { options, correctIndex }, en, ar);

    private static ExamQuestion TrueFalse(
        Level level, ExerciseSkill skill, string prompt, bool correct, string en, string ar) =>
        Make(level, skill, ExerciseType.TrueFalse, prompt, new { correct }, en, ar);

    private static ExamQuestion Fill(
        Level level, ExerciseSkill skill, string prompt, string[][] blanks, string en, string ar) =>
        Make(level, skill, ExerciseType.FillBlank, prompt, new { blanks }, en, ar);

    private static ExamQuestion Order(
        Level level, ExerciseSkill skill, string prompt, string[] solution, string en, string ar) =>
        Make(level, skill, ExerciseType.Ordering, prompt, new { solution }, en, ar);

    private static ExamQuestion Match(
        Level level, ExerciseSkill skill, string prompt, (string Left, string Right)[] pairs, string en, string ar) =>
        Make(
            level, skill, ExerciseType.Matching, prompt,
            new { pairs = pairs.Select(p => new { left = p.Left, right = p.Right }).ToArray() }, en, ar);

    private static ExamQuestion Make(
        Level level, ExerciseSkill skill, ExerciseType type, string prompt, object payload, string en, string ar) =>
        new()
        {
            LevelId = level.Id,
            Skill = skill,
            Type = type,
            Prompt = prompt,
            PayloadJson = Json(payload),
            ExplanationEn = en,
            ExplanationAr = ar
        };
}