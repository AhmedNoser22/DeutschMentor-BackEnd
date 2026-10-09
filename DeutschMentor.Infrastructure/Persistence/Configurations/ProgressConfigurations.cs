namespace DeutschMentor.Infrastructure.Persistence.Configurations;

public sealed class UserLessonProgressConfiguration : IEntityTypeConfiguration<UserLessonProgress>
{
    public void Configure(EntityTypeBuilder<UserLessonProgress> b)
    {
        b.ToTable("UserLessonProgress");
        b.HasKey(x => x.Id);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Lesson>().WithMany().HasForeignKey(x => x.LessonId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.UserId, x.LessonId }).IsUnique();
    }
}

public sealed class LevelPassConfiguration : IEntityTypeConfiguration<LevelPass>
{
    public void Configure(EntityTypeBuilder<LevelPass> b)
    {
        b.ToTable("LevelPasses");
        b.HasKey(x => x.Id);
        b.Property(x => x.Source).HasConversion<string>().HasMaxLength(20);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Level>().WithMany().HasForeignKey(x => x.LevelId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.UserId, x.LevelId }).IsUnique();
    }
}

public sealed class ReviewItemConfiguration : IEntityTypeConfiguration<ReviewItem>
{
    public void Configure(EntityTypeBuilder<ReviewItem> b)
    {
        b.ToTable("ReviewItems");
        b.HasKey(x => x.Id);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<VocabularyItem>().WithMany().HasForeignKey(x => x.VocabularyItemId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.UserId, x.VocabularyItemId }).IsUnique();
        b.HasIndex(x => new { x.UserId, x.DueAtUtc });
    }
}