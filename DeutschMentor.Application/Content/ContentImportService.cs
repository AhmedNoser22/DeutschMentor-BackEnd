namespace DeutschMentor.Application.Content;

public interface IContentImportService
{
    ImportReport Check(ContentPack pack);
    Task<ImportReport> ImportAsync(ContentPack pack, bool publish, CancellationToken ct);
}

public sealed class ContentImportService(IApplicationDbContext db) : IContentImportService
{
    public ImportReport Check(ContentPack pack) => ContentValidator.Validate(pack);

    public async Task<ImportReport> ImportAsync(ContentPack pack, bool publish, CancellationToken ct)
    {
        var report = ContentValidator.Validate(pack);
        if (report.Errors.Count > 0)
        {
            return report;
        }

        var cefr = Enum.Parse<CefrLevel>(pack.Level!);
        var level = await db.Levels.FirstOrDefaultAsync(l => l.Code == cefr, ct);
        if (level is null)
        {
            report.Error("pack", "the level does not exist in the database.");
            return report;
        }

        var modules = await db.Modules.Where(m => m.LevelId == level.Id).ToListAsync(ct);

        foreach (var pm in pack.Modules ?? [])
        {
            var module = modules.FirstOrDefault(m => m.Order == pm.Order);
            if (module is null)
            {
                module = new Module { LevelId = level.Id, Order = pm.Order };
                db.Modules.Add(module);
                modules.Add(module);
            }

            module.Title = pm.Title!.Trim();
            module.TitleEn = pm.TitleEn!.Trim();

            var units = await db.Units.Where(u => u.ModuleId == module.Id).ToListAsync(ct);
            foreach (var pu in pm.Units ?? [])
            {
                var unit = units.FirstOrDefault(u => u.Order == pu.Order);
                if (unit is null)
                {
                    unit = new Unit { ModuleId = module.Id, Order = pu.Order };
                    db.Units.Add(unit);
                    units.Add(unit);
                }

                unit.Title = pu.Title!.Trim();
                unit.TitleEn = pu.TitleEn!.Trim();

                foreach (var pl in pu.Lessons ?? [])
                {
                    await ImportLessonAsync(unit, pl, publish, report, ct);
                }
            }
        }

        await ImportExamAsync(level, pack.Exam ?? [], report, ct);

        try
        {
            await db.SaveChangesAsync(ct);
            report.Imported = true;
        }
        catch (DbUpdateException)
        {
            report.Imported = false;
            report.Error("database", "the file conflicts with existing content (module, unit, lesson or item order). Nothing was saved.");
        }

        return report;
    }

    private async Task ImportLessonAsync(Unit unit, PackLesson pl, bool publish, ImportReport report, CancellationToken ct)
    {
        var lesson = await db.Lessons.FirstOrDefaultAsync(l => l.Code == pl.Code, ct);
        var isNew = lesson is null;

        if (lesson is null)
        {
            lesson = new Lesson { Code = pl.Code!, Status = publish ? ContentStatus.Published : ContentStatus.Draft };
            db.Lessons.Add(lesson);
            report.Count("new lessons");
        }
        else if (publish)
        {
            lesson.Status = ContentStatus.Published;
        }

        lesson.UnitId = unit.Id;
        lesson.Order = pl.Order;
        lesson.Title = pl.Title!.Trim();
        lesson.TitleEn = pl.TitleEn!.Trim();
        lesson.TitleAr = pl.TitleAr!.Trim();
        lesson.EstimatedMinutes = pl.Minutes;

        var words = isNew ? new List<VocabularyItem>() : await db.VocabularyItems.Where(v => v.LessonId == lesson.Id).ToListAsync(ct);
        var vocabulary = pl.Vocabulary ?? [];
        for (var i = 0; i < vocabulary.Count; i++)
        {
            var w = vocabulary[i];
            var row = words.FirstOrDefault(x => x.Order == i + 1);
            if (row is null)
            {
                row = new VocabularyItem { LessonId = lesson.Id, Order = i + 1 };
                db.VocabularyItems.Add(row);
            }

            row.German = w.German!.Trim();
            row.Article = ContentValidator.ParseArticle(w.Article);
            row.PartOfSpeech = Enum.Parse<PartOfSpeech>(w.Pos!);
            row.English = w.English!.Trim();
            row.Arabic = w.Arabic!.Trim();
            row.ExampleGerman = w.Example!.Trim();
            row.ExampleEnglish = w.ExampleEn!.Trim();
        }

        var topics = isNew ? new List<GrammarTopic>() : await db.GrammarTopics.Where(g => g.LessonId == lesson.Id).ToListAsync(ct);
        var grammar = pl.Grammar ?? [];
        for (var i = 0; i < grammar.Count; i++)
        {
            var g = grammar[i];
            var row = topics.FirstOrDefault(x => x.Order == i + 1);
            if (row is null)
            {
                row = new GrammarTopic { LessonId = lesson.Id, Order = i + 1 };
                db.GrammarTopics.Add(row);
            }

            GrammarTable? table = g.Table is null
                ? null
                : new GrammarTable(
                    g.Table.Headers ?? [],
                    (g.Table.Rows ?? []).Select(r => (IReadOnlyList<string>)r).ToList(),
                    g.Table.Highlight ?? []);

            row.Title = g.Title!.Trim();
            row.Summary = g.Summary!.Trim();
            row.ContentJson = JsonSerializer.Serialize(new GrammarContent(table, g.NoteEn, g.NoteAr), JsonSerializerOptions.Web);
        }

        var exercises = isNew ? new List<Exercise>() : await db.Exercises.Where(e => e.LessonId == lesson.Id).ToListAsync(ct);
        var practice = pl.Exercises ?? [];
        for (var i = 0; i < practice.Count; i++)
        {
            var row = exercises.FirstOrDefault(x => x.ReadingPassageId == null && x.Order == i + 1);
            if (row is null)
            {
                row = new Exercise { LessonId = lesson.Id, Order = i + 1 };
                db.Exercises.Add(row);
            }

            MapExercise(row, practice[i]);
        }

        var passages = isNew ? new List<ReadingPassage>() : await db.ReadingPassages.Where(p => p.LessonId == lesson.Id).ToListAsync(ct);
        var readings = pl.Reading ?? [];
        for (var i = 0; i < readings.Count; i++)
        {
            var pr = readings[i];
            var passage = passages.FirstOrDefault(x => x.Order == i + 1);
            if (passage is null)
            {
                passage = new ReadingPassage { LessonId = lesson.Id, Order = i + 1 };
                db.ReadingPassages.Add(passage);
            }

            passage.Title = pr.Title!.Trim();
            passage.BodyJson = JsonSerializer.Serialize(
                (pr.Lines ?? []).Select(l => new { speaker = l.Speaker, text = l.Text!.Trim() }).ToList(),
                JsonSerializerOptions.Web);

            var questions = pr.Questions ?? [];
            for (var j = 0; j < questions.Count; j++)
            {
                var row = exercises.FirstOrDefault(x => x.ReadingPassageId == passage.Id && x.Order == j + 1);
                if (row is null)
                {
                    row = new Exercise { LessonId = lesson.Id, ReadingPassageId = passage.Id, Order = j + 1 };
                    db.Exercises.Add(row);
                }

                MapExercise(row, questions[j]);
            }
        }

        var tasks = isNew ? new List<WritingTask>() : await db.WritingTasks.Where(t => t.LessonId == lesson.Id).ToListAsync(ct);
        var writing = pl.Writing ?? [];
        for (var i = 0; i < writing.Count; i++)
        {
            var pw = writing[i];
            var row = tasks.FirstOrDefault(x => x.Order == i + 1);
            if (row is null)
            {
                row = new WritingTask { LessonId = lesson.Id, Order = i + 1 };
                db.WritingTasks.Add(row);
            }

            row.Title = pw.Title!.Trim();
            row.Prompt = pw.Prompt!.Trim();
            row.SampleAnswer = pw.Sample!.Trim();
            row.MinWords = pw.MinWords;
            row.PassPercent = pw.Pass;
            row.RulesJson = ContentValidator.RulesJson(pw);
        }
    }

    private async Task ImportExamAsync(Level level, List<PackExam> exam, ImportReport report, CancellationToken ct)
    {
        if (exam.Count == 0)
        {
            return;
        }

        var existing = await db.ExamQuestions.Where(q => q.LevelId == level.Id).ToListAsync(ct);
        var byKey = existing.ToDictionary(q => Key(q.Skill, q.Type, q.Prompt, q.PayloadJson), q => q);

        foreach (var pe in exam)
        {
            var skill = Enum.Parse<ExerciseSkill>(pe.Skill!);
            var type = Enum.Parse<ExerciseType>(pe.Type!);
            var payload = PayloadJson(pe);
            var key = Key(skill, type, pe.Prompt!.Trim(), payload);

            if (!byKey.TryGetValue(key, out var row))
            {
                row = new ExamQuestion { LevelId = level.Id, Skill = skill, Type = type, Prompt = pe.Prompt!.Trim(), PayloadJson = payload };
                db.ExamQuestions.Add(row);
                byKey[key] = row;
                report.Count("new exam questions");
            }

            row.ExplanationEn = pe.ExplainEn!.Trim();
            row.ExplanationAr = pe.ExplainAr!.Trim();
            row.IsActive = true;
        }
    }

    private static string Key(ExerciseSkill skill, ExerciseType type, string prompt, string payload) =>
        $"{skill}|{type}|{prompt}|{payload}".ToLowerInvariant();

    private static void MapExercise(Exercise row, PackExercise p)
    {
        row.Type = Enum.Parse<ExerciseType>(p.Type!);
        row.Prompt = p.Prompt!.Trim();
        row.PayloadJson = PayloadJson(p);
        row.ExplanationEn = p.ExplainEn!.Trim();
        row.ExplanationAr = p.ExplainAr!.Trim();
    }

    private static string PayloadJson(PackExercise p)
    {
        object payload = Enum.Parse<ExerciseType>(p.Type!) switch
        {
            ExerciseType.MultipleChoice => new { options = p.Options, correctIndex = p.Options!.IndexOf(p.Correct!) },
            ExerciseType.TrueFalse => new { correct = p.Answer!.Value },
            ExerciseType.FillBlank => new { blanks = p.Blanks },
            ExerciseType.Ordering => new { solution = p.Solution },
            _ => new { pairs = p.Pairs!.Select(x => new { left = x[0], right = x[1] }).ToList() }
        };

        return JsonSerializer.Serialize(payload, JsonSerializerOptions.Web);
    }
}