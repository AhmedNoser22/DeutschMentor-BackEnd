namespace DeutschMentor.Application.Content;

public sealed class ImportReport
{
    public List<string> Errors { get; } = [];
    public List<string> Warnings { get; } = [];
    public Dictionary<string, int> Counts { get; } = [];
    public bool Imported { get; set; }

    internal void Error(string where, string message) => Errors.Add($"{where}: {message}");
    internal void Warn(string where, string message) => Warnings.Add($"{where}: {message}");
    internal void Count(string key) => Counts[key] = Counts.GetValueOrDefault(key) + 1;
}

internal static class ContentValidator
{
    private static readonly Regex ArabicLetters = new(@"[\u0600-\u06FF]");
    private static readonly string[] Endings = [".", "?", "!", "…", "\"", "“"];

    public static ImportReport Validate(ContentPack pack)
    {
        var r = new ImportReport();

        if (pack.SchemaVersion != 1)
        {
            r.Error("pack", "schemaVersion must be 1.");
        }

        if (pack.Level is null || !Enum.GetNames<CefrLevel>().Contains(pack.Level))
        {
            r.Error("pack", "level must be A1, A2, B1, B2 or C1.");
            return r;
        }

        var codes = new HashSet<string>();
        var moduleOrders = new HashSet<int>();
        var seenWords = new HashSet<string>();

        foreach (var module in pack.Modules ?? [])
        {
            var mw = $"module {module.Order}";
            if (!moduleOrders.Add(module.Order)) r.Error(mw, "duplicate module order.");
            if (Blank(module.Title) || Blank(module.TitleEn)) r.Error(mw, "title and titleEn are required.");

            var unitOrders = new HashSet<int>();
            foreach (var unit in module.Units ?? [])
            {
                var uw = $"unit {module.Order}.{unit.Order}";
                if (!unitOrders.Add(unit.Order)) r.Error(uw, "duplicate unit order.");
                if (Blank(unit.Title) || Blank(unit.TitleEn)) r.Error(uw, "title and titleEn are required.");

                var lessonOrders = new HashSet<int>();
                foreach (var lesson in unit.Lessons ?? [])
                {
                    CheckLesson(lesson, module.Order, pack.Level, lessonOrders, codes, seenWords, r);
                }
            }
        }

        var examKeys = new HashSet<string>();
        var perSkill = new Dictionary<ExerciseSkill, int>();
        var number = 0;

        foreach (var q in pack.Exam ?? [])
        {
            var where = $"exam #{++number}";
            if (!Enum.TryParse<ExerciseSkill>(q.Skill, out var skill) || !Enum.IsDefined(skill))
            {
                r.Error(where, "skill must be Vocabulary, Grammar, Reading or Writing.");
            }
            else
            {
                perSkill[skill] = perSkill.GetValueOrDefault(skill) + 1;
            }

            CheckExercise(q, where, r);

            var key = JsonSerializer.Serialize(new { q.Skill, q.Type, q.Prompt, q.Options, q.Correct, q.Answer, q.Blanks, q.Solution, q.Pairs }).ToLowerInvariant();
            if (!examKeys.Add(key)) r.Error(where, "duplicate exam question.");
            r.Count("exam questions");
        }

        if (number > 0)
        {
            foreach (var skill in Enum.GetValues<ExerciseSkill>())
            {
                if (perSkill.GetValueOrDefault(skill) < 4) r.Warn("exam", $"fewer than 4 questions for {skill}.");
            }
        }

        return r;
    }

    internal static Article? ParseArticle(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "der" => Article.Der,
        "die" => Article.Die,
        "das" => Article.Das,
        _ => null
    };

    private static void CheckLesson(
        PackLesson lesson, int moduleOrder, string level, HashSet<int> orders,
        HashSet<string> codes, HashSet<string> seenWords, ImportReport r)
    {
        var code = lesson.Code ?? string.Empty;
        var w = Blank(code) ? "lesson" : code;

        if (!orders.Add(lesson.Order)) r.Error(w, "duplicate lesson order.");

        var match = Regex.Match(code, $@"^{level}-(\d{{1,2}})-(\d{{2}})$");
        if (!match.Success) r.Error(w, $"code must look like {level}-<module>-<nn>.");
        else if (int.Parse(match.Groups[1].Value) != moduleOrder) r.Error(w, "the module number in the code must equal the module order.");
        if (!codes.Add(code)) r.Error(w, "duplicate lesson code.");

        if (Blank(lesson.Title) || Blank(lesson.TitleEn) || Blank(lesson.TitleAr)) r.Error(w, "title, titleEn and titleAr are required.");
        if (!Blank(lesson.TitleAr) && !HasArabic(lesson.TitleAr)) r.Error(w, "titleAr must contain Arabic text.");
        if (lesson.Minutes is < 5 or > 60) r.Error(w, "minutes must be 5 to 60.");
        r.Count("lessons");

        var vocabulary = lesson.Vocabulary ?? [];
        if (vocabulary.Count < 4) r.Warn(w, "fewer than 4 vocabulary items.");

        var local = new HashSet<string>();
        for (var i = 0; i < vocabulary.Count; i++)
        {
            var ww = $"{w} word #{i + 1}";
            CheckWord(vocabulary[i], ww, r);
            var key = $"{vocabulary[i].Article}|{vocabulary[i].German?.Trim().ToLowerInvariant()}";
            if (!local.Add(key)) r.Error(ww, "duplicate word in the lesson.");
            else if (!seenWords.Add(key)) r.Warn(ww, "word already used in another lesson of this file.");
            r.Count("words");
        }

        var number = 0;
        foreach (var g in lesson.Grammar ?? [])
        {
            CheckGrammar(g, $"{w} grammar #{++number}", r);
            r.Count("grammar");
        }

        number = 0;
        foreach (var e in lesson.Exercises ?? [])
        {
            CheckExercise(e, $"{w} exercise #{++number}", r);
            r.Count("exercises");
        }

        number = 0;
        foreach (var p in lesson.Reading ?? [])
        {
            var pw = $"{w} reading #{++number}";
            if (Blank(p.Title)) r.Error(pw, "title is required.");
            if ((p.Lines ?? []).Count < 2 || (p.Lines ?? []).Any(l => Blank(l.Text))) r.Error(pw, "needs at least 2 lines with text.");
            if ((p.Questions ?? []).Count == 0) r.Error(pw, "needs at least 1 question.");

            var q = 0;
            foreach (var question in p.Questions ?? [])
            {
                CheckExercise(question, $"{pw} question #{++q}", r);
            }

            r.Count("readings");
        }

        number = 0;
        foreach (var t in lesson.Writing ?? [])
        {
            CheckWriting(t, $"{w} writing #{++number}", r);
            r.Count("writing tasks");
        }
    }

    private static void CheckWord(PackWord word, string where, ImportReport r)
    {
        if (Blank(word.German) || Blank(word.English) || Blank(word.Arabic) || Blank(word.Example) || Blank(word.ExampleEn))
        {
            r.Error(where, "german, english, arabic, example and exampleEn are required.");
        }

        var german = (word.German ?? string.Empty).Trim();
        var validPos = Enum.TryParse<PartOfSpeech>(word.Pos, out var pos) && Enum.IsDefined(pos);
        if (!validPos) r.Error(where, $"unknown pos '{word.Pos}'.");

        var article = ParseArticle(word.Article);
        if (word.Article is not null && article is null) r.Error(where, "article must be der, die or das.");
        if (validPos && pos == PartOfSpeech.Noun && article is null) r.Error(where, "a noun needs an article.");
        if (validPos && pos != PartOfSpeech.Noun && article is not null) r.Error(where, "only nouns have an article.");
        if (validPos && pos == PartOfSpeech.Noun && german.Length > 0 && !char.IsUpper(german[0])) r.Error(where, "a German noun starts with a capital letter.");
        if (validPos && pos == PartOfSpeech.Verb && !german.EndsWith('n')) r.Warn(where, "a verb infinitive usually ends with -n.");
        if (!Blank(word.Arabic) && !HasArabic(word.Arabic)) r.Error(where, "arabic must contain Arabic text.");
        if (HasArabic(word.English) || HasArabic(word.ExampleEn)) r.Error(where, "english fields must not contain Arabic.");

        var example = (word.Example ?? string.Empty).Trim();
        if (example.Length > 0 && !Endings.Any(example.EndsWith)) r.Warn(where, "example should end with punctuation.");
        if (example.Length > 0 && !char.IsUpper(example[0])) r.Warn(where, "example should start with a capital letter.");
    }

    private static void CheckGrammar(PackGrammar g, string where, ImportReport r)
    {
        if (Blank(g.Title) || Blank(g.Summary)) r.Error(where, "title and summary are required.");

        if (g.Table is { } table)
        {
            var rows = table.Rows ?? [];
            if (rows.Count == 0) r.Error(where, "table has no rows.");
            else
            {
                var width = rows[0].Count;
                if (rows.Any(row => row.Count != width)) r.Error(where, "all table rows need the same number of cells.");
                if ((table.Headers ?? []).Count > 0 && table.Headers!.Count != width) r.Error(where, "headers and rows need the same number of columns.");
                if ((table.Highlight ?? []).Any(i => i < 0 || i >= width)) r.Error(where, "highlight column is out of range.");
            }
        }

        if (Blank(g.NoteEn) && Blank(g.NoteAr)) r.Warn(where, "no note.");
        if (!Blank(g.NoteAr) && !HasArabic(g.NoteAr)) r.Error(where, "noteAr must contain Arabic text.");
        if (HasArabic(g.NoteEn)) r.Error(where, "noteEn must not contain Arabic.");
    }

    internal static void CheckExercise(PackExercise e, string where, ImportReport r)
    {
        if (!Enum.TryParse<ExerciseType>(e.Type, out var type) || !Enum.IsDefined(type))
        {
            r.Error(where, $"unknown type '{e.Type}'.");
            return;
        }

        if (Blank(e.Prompt)) r.Error(where, "prompt is required.");
        if (Blank(e.ExplainEn)) r.Error(where, "explainEn is required.");
        if (!HasArabic(e.ExplainAr)) r.Error(where, "explainAr must contain Arabic text.");
        if (HasArabic(e.ExplainEn) || HasArabic(e.Prompt)) r.Error(where, "prompt and explainEn must not contain Arabic.");

        switch (type)
        {
            case ExerciseType.MultipleChoice:
                var options = e.Options ?? [];
                if (options.Count is < 2 or > 6) r.Error(where, "options must have 2 to 6 items.");
                if (options.Any(Blank)) r.Error(where, "an option is empty.");
                if (options.Select(o => o.Trim().ToLowerInvariant()).Distinct().Count() != options.Count) r.Error(where, "options contain duplicates.");
                if (options.Count(o => o == e.Correct) != 1) r.Error(where, "correct must match exactly one option.");
                break;

            case ExerciseType.TrueFalse:
                if (e.Answer is null) r.Error(where, "answer must be true or false.");
                break;

            case ExerciseType.FillBlank:
                var blanks = e.Blanks ?? [];
                if (blanks.Count == 0) r.Error(where, "blanks is empty.");
                if ((e.Prompt ?? string.Empty).Split("___").Length - 1 != blanks.Count) r.Error(where, "number of ___ in prompt must equal number of blanks.");
                if (blanks.Any(b => b.Count == 0 || b.Any(Blank))) r.Error(where, "every blank needs at least one non-empty answer.");
                break;

            case ExerciseType.Ordering:
                var solution = e.Solution ?? [];
                if (solution.Count < 2 || solution.Any(Blank)) r.Error(where, "solution needs at least 2 non-empty words.");
                break;

            case ExerciseType.Matching:
                var pairs = e.Pairs ?? [];
                if (pairs.Count is < 2 or > 8 || pairs.Any(p => p.Count != 2 || Blank(p[0]) || Blank(p[1])))
                {
                    r.Error(where, "pairs needs 2 to 8 items of [left, right].");
                }
                else if (pairs.Select(p => p[0].Trim().ToLowerInvariant()).Distinct().Count() != pairs.Count
                    || pairs.Select(p => p[1].Trim().ToLowerInvariant()).Distinct().Count() != pairs.Count)
                {
                    r.Error(where, "pairs must have unique lefts and unique rights.");
                }
                break;
        }
    }

    private static void CheckWriting(PackWriting w, string where, ImportReport r)
    {
        if (Blank(w.Title) || Blank(w.Prompt) || Blank(w.Sample)) r.Error(where, "title, prompt and sample are required.");
        if (w.MinWords is < 1 or > 100) r.Error(where, "minWords must be 1 to 100.");
        if (w.Pass is < 40 or > 100) r.Error(where, "pass must be 40 to 100.");

        var groups = w.Groups ?? [];
        if (groups.Count == 0) r.Error(where, "groups is empty.");
        if (groups.Any(g => Blank(g.Label) || (g.AnyOf ?? []).Count == 0 || (g.AnyOf ?? []).Any(Blank))) r.Error(where, "every group needs a label and non-empty anyOf values.");

        if (Blank(w.Sample) || groups.Count == 0) return;

        var task = new WritingTask { MinWords = w.MinWords, PassPercent = w.Pass, RulesJson = RulesJson(w) };
        var evaluation = WritingEvaluator.Evaluate(task, w.Sample);
        var allMet = evaluation.Checks.Take(groups.Count).All(c => c.Met);

        if (!allMet || !evaluation.Passed)
        {
            r.Error(where, $"the sample answer fails its own rules (score {evaluation.ScorePercent}).");
        }
    }

    internal static string RulesJson(PackWriting w) => JsonSerializer.Serialize(
        new { groups = (w.Groups ?? []).Select(g => new { label = g.Label, anyOf = g.AnyOf ?? new List<string>() }).ToList() },
        JsonSerializerOptions.Web);

    private static bool Blank(string? value) => string.IsNullOrWhiteSpace(value);

    private static bool HasArabic(string? value) => !string.IsNullOrEmpty(value) && ArabicLetters.IsMatch(value);
}