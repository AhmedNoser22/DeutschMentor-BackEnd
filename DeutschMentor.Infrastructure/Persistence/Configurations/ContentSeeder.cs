namespace DeutschMentor.Infrastructure.Persistence;

public static class ContentSeeder
{
    public static async Task EnsureLevelsAsync(ApplicationDbContext db)
    {
        if (await db.Levels.AnyAsync())
        {
            return;
        }

        db.Levels.AddRange(
            NewLevel(CefrLevel.A1, 1, "Beginner", "First words and everyday phrases."),
            NewLevel(CefrLevel.A2, 2, "Elementary", "Simple conversations about daily life."),
            NewLevel(CefrLevel.B1, 3, "Intermediate", "Handle most situations when travelling and working."),
            NewLevel(CefrLevel.B2, 4, "Upper-intermediate", "Discuss complex topics with confidence."),
            NewLevel(CefrLevel.C1, 5, "Advanced", "Express yourself fluently and precisely."));

        await db.SaveChangesAsync();
    }

    public static async Task SeedSampleContentAsync(ApplicationDbContext db)
    {
        if (await db.Lessons.AnyAsync())
        {
            return;
        }

        var a1 = await db.Levels.FirstAsync(l => l.Code == CefrLevel.A1);
        var a2 = await db.Levels.FirstAsync(l => l.Code == CefrLevel.A2);

        var m1 = new Module { LevelId = a1.Id, Order = 1, Title = "Erste Schritte", TitleEn = "First steps" };
        var u1 = new Domain.Entities.Unit { ModuleId = m1.Id, Order = 1, Title = "Begrüßen", TitleEn = "Greetings" };
        var hello = NewLesson(u1, 1, "A1-1-01", "Hallo und Tschüss", "Hello and goodbye", "التحيات والوداع", 10);
        var intro = NewLesson(u1, 2, "A1-1-02", "Sich vorstellen", "Introducing yourself", "التعريف بالنفس", 12);

        var m2 = new Module { LevelId = a2.Id, Order = 1, Title = "Essen und Trinken", TitleEn = "Food and drink" };
        var u2 = new Domain.Entities.Unit { ModuleId = m2.Id, Order = 1, Title = "Im Restaurant", TitleEn = "In the restaurant" };
        var menu = NewLesson(u2, 1, "A2-1-01", "Die Speisekarte", "The menu", "قائمة الطعام", 10);
        var drinks = NewLesson(u2, 2, "A2-1-02", "Getränke bestellen", "Ordering drinks", "طلب المشروبات", 12);
        var bill = NewLesson(u2, 3, "A2-1-03", "Die Rechnung", "The bill", "الفاتورة", 10);

        db.AddRange(m1, u1, hello, intro, m2, u2, menu, drinks, bill);

        db.VocabularyItems.AddRange(
            Word(hello, 1, "Hallo", null, PartOfSpeech.Phrase, "hello", "مرحبًا", "Hallo, wie geht's?", "Hello, how are you?"),
            Word(hello, 2, "Tschüss", null, PartOfSpeech.Phrase, "bye", "مع السلامة", "Tschüss, bis morgen!", "Bye, see you tomorrow!"),
            Word(hello, 3, "Guten Morgen", null, PartOfSpeech.Phrase, "good morning", "صباح الخير", "Guten Morgen, Frau Meier.", "Good morning, Mrs Meier."),
            Word(hello, 4, "Danke", null, PartOfSpeech.Phrase, "thank you", "شكرًا", "Danke für die Hilfe.", "Thanks for the help."),

            Word(intro, 1, "Name", Article.Der, PartOfSpeech.Noun, "name", "الاسم", "Mein Name ist Omar.", "My name is Omar."),
            Word(intro, 2, "heißen", null, PartOfSpeech.Verb, "to be called", "يُدعى", "Ich heiße Omar.", "My name is Omar."),
            Word(intro, 3, "kommen", null, PartOfSpeech.Verb, "to come", "يأتي", "Ich komme aus Ägypten.", "I come from Egypt."),
            Word(intro, 4, "wohnen", null, PartOfSpeech.Verb, "to live", "يسكن", "Ich wohne in Kairo.", "I live in Cairo."),

            Word(menu, 1, "Speisekarte", Article.Die, PartOfSpeech.Noun, "menu", "قائمة الطعام", "Die Speisekarte, bitte.", "The menu, please."),
            Word(menu, 2, "Suppe", Article.Die, PartOfSpeech.Noun, "soup", "الشوربة", "Ich nehme die Suppe.", "I will have the soup."),
            Word(menu, 3, "Fleisch", Article.Das, PartOfSpeech.Noun, "meat", "اللحم", "Ich esse kein Fleisch.", "I do not eat meat."),
            Word(menu, 4, "Salat", Article.Der, PartOfSpeech.Noun, "salad", "السلطة", "Der Salat ist frisch.", "The salad is fresh."),

            Word(drinks, 1, "Wasser", Article.Das, PartOfSpeech.Noun, "water", "الماء", "Ich möchte ein Wasser, bitte.", "I would like a water, please."),
            Word(drinks, 2, "Kaffee", Article.Der, PartOfSpeech.Noun, "coffee", "القهوة", "Er trinkt einen Kaffee.", "He drinks a coffee."),
            Word(drinks, 3, "Milch", Article.Die, PartOfSpeech.Noun, "milk", "الحليب", "Möchten Sie Milch im Kaffee?", "Would you like milk in your coffee?"),
            Word(drinks, 4, "bestellen", null, PartOfSpeech.Verb, "to order", "يطلب", "Wir bestellen zwei Tassen Tee.", "We order two cups of tea."),

            Word(bill, 1, "Rechnung", Article.Die, PartOfSpeech.Noun, "bill, check", "الفاتورة", "Die Rechnung, bitte!", "The bill, please!"),
            Word(bill, 2, "bezahlen", null, PartOfSpeech.Verb, "to pay", "يدفع", "Ich bezahle mit Karte.", "I pay by card."),
            Word(bill, 3, "Trinkgeld", Article.Das, PartOfSpeech.Noun, "tip", "البقشيش", "Hier ist das Trinkgeld.", "Here is the tip."),
            Word(bill, 4, "zusammen", null, PartOfSpeech.Adverb, "together", "معًا", "Zusammen oder getrennt?", "Together or separately?"));

        db.GrammarTopics.AddRange(
            Topic(hello, 1, "Greetings through the day", "Which greeting fits which time.",
                Content(["German", "When"],
                    [["Guten Morgen", "In the morning"], ["Guten Tag", "During the day"], ["Guten Abend", "In the evening"], ["Gute Nacht", "Before going to bed"]],
                    [0],
                    "Use Guten Tag for most daytime situations.",
                    "نستخدم «Guten Tag» في معظم مواقف النهار.")),

            Topic(intro, 1, "sein", "The verb \"to be\".",
                Content([],
                    [["ich", "bin"], ["du", "bist"], ["er / sie / es", "ist"], ["wir", "sind"], ["ihr", "seid"], ["sie / Sie", "sind"]],
                    [1],
                    "The verb sein is irregular. Learn the forms by heart.",
                    "الفعل «sein» بمعنى «يكون» شاذ، لذلك احفظ تصريفاته كما هي.")),

            Topic(drinks, 1, "möchten", "A polite way to say \"would like\".",
                Content([],
                    [["ich", "möchte"], ["du", "möchtest"], ["er / sie / es", "möchte"], ["wir", "möchten"], ["ihr", "möchtet"], ["sie / Sie", "möchten"]],
                    [1],
                    "Möchten takes the second position in the sentence. The thing you order goes at the end.",
                    "«möchten» تعني «أودّ» بشكل مهذّب. يأتي الفعل في المركز الثاني من الجملة، ويأتي الشيء المطلوب في آخرها.")),

            Topic(drinks, 2, "The thing you order", "After möchten, the object takes the accusative case.",
                Content(["Gender", "Noun", "Accusative"],
                    [["der", "der Kaffee", "einen Kaffee"], ["die", "die Milch", "eine Milch"], ["das", "das Wasser", "ein Wasser"]],
                    [2],
                    "Only the masculine (der) changes: ein becomes einen. Feminine and neuter stay the same.",
                    "المذكّر (der) فقط هو الذي يتغيّر: «ein» تصبح «einen». المؤنّث والمحايد يبقيان كما هما.")));

        await db.SaveChangesAsync();
    }

    private static Level NewLevel(CefrLevel code, int order, string title, string description) =>
        new() { Code = code, Order = order, Title = title, Description = description, PassMarkPercent = 70 };

    private static Lesson NewLesson(Domain.Entities.Unit unit, int order, string code, string title, string titleEn, string titleAr, int minutes) =>
        new()
        {
            UnitId = unit.Id,
            Order = order,
            Code = code,
            Title = title,
            TitleEn = titleEn,
            TitleAr = titleAr,
            EstimatedMinutes = minutes,
            Status = ContentStatus.Published
        };

    private static VocabularyItem Word(
        Lesson lesson, int order, string german, Article? article, PartOfSpeech partOfSpeech,
        string english, string arabic, string exampleGerman, string exampleEnglish) =>
        new()
        {
            LessonId = lesson.Id,
            Order = order,
            German = german,
            Article = article,
            PartOfSpeech = partOfSpeech,
            English = english,
            Arabic = arabic,
            ExampleGerman = exampleGerman,
            ExampleEnglish = exampleEnglish
        };

    private static GrammarTopic Topic(Lesson lesson, int order, string title, string summary, string contentJson) =>
        new() { LessonId = lesson.Id, Order = order, Title = title, Summary = summary, ContentJson = contentJson };

    private static string Content(string[] headers, string[][] rows, int[] highlight, string noteEn, string noteAr) =>
        JsonSerializer.Serialize(
            new GrammarContent(new GrammarTable(headers, rows, highlight), noteEn, noteAr),
            JsonSerializerOptions.Web);
}