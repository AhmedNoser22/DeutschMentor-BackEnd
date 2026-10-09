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

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}