namespace DeutschMentor.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<PendingRegistration> PendingRegistrations { get; }
    DbSet<PasswordResetCode> PasswordResetCodes { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    DbSet<Level> Levels { get; }
    DbSet<Module> Modules { get; }
    DbSet<Unit> Units { get; }
    DbSet<Lesson> Lessons { get; }
    DbSet<VocabularyItem> VocabularyItems { get; }
    DbSet<GrammarTopic> GrammarTopics { get; }
    DbSet<UserLessonProgress> LessonProgress { get; }
    DbSet<LevelPass> LevelPasses { get; }
    DbSet<ReviewItem> ReviewItems { get; }

    DbSet<Exercise> Exercises { get; }
    DbSet<ExerciseAttempt> ExerciseAttempts { get; }
    DbSet<ReadingPassage> ReadingPassages { get; }
    DbSet<WritingTask> WritingTasks { get; }
    DbSet<WritingSubmission> WritingSubmissions { get; }

    DbSet<ExamQuestion> ExamQuestions { get; }
    DbSet<ExamAttempt> ExamAttempts { get; }
    DbSet<ExamAnswer> ExamAnswers { get; }
    DbSet<Certificate> Certificates { get; }

    DbSet<ReviewLog> ReviewLogs { get; }
    DbSet<DrillAttempt> DrillAttempts { get; }
    DbSet<UserAchievement> UserAchievements { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}