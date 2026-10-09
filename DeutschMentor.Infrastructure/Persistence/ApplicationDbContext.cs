namespace DeutschMentor.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, TimeProvider clock)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<PendingRegistration> PendingRegistrations => Set<PendingRegistration>();
    public DbSet<PasswordResetCode> PasswordResetCodes => Set<PasswordResetCode>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Level> Levels => Set<Level>();
    public DbSet<Module> Modules => Set<Module>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<VocabularyItem> VocabularyItems => Set<VocabularyItem>();
    public DbSet<GrammarTopic> GrammarTopics => Set<GrammarTopic>();
    public DbSet<UserLessonProgress> LessonProgress => Set<UserLessonProgress>();
    public DbSet<LevelPass> LevelPasses => Set<LevelPass>();
    public DbSet<ReviewItem> ReviewItems => Set<ReviewItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        foreach (var entry in ChangeTracker.Entries<BaseEntity>().Where(e => e.State == EntityState.Added))
        {
            entry.Entity.CreatedAtUtc = now;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}