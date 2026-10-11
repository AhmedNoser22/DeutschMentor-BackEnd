namespace DeutschMentor.Infrastructure.Persistence.Configurations;

public sealed class ReviewLogConfiguration : IEntityTypeConfiguration<ReviewLog>
{
    public void Configure(EntityTypeBuilder<ReviewLog> b)
    {
        b.ToTable("ReviewLogs");
        b.HasKey(x => x.Id);
        b.Property(x => x.Rating).HasConversion<string>().HasMaxLength(10);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<VocabularyItem>().WithMany().HasForeignKey(x => x.VocabularyItemId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.UserId, x.CreatedAtUtc });
    }
}

public sealed class DrillAttemptConfiguration : IEntityTypeConfiguration<DrillAttempt>
{
    public void Configure(EntityTypeBuilder<DrillAttempt> b)
    {
        b.ToTable("DrillAttempts");
        b.HasKey(x => x.Id);
        b.Property(x => x.ChosenArticle).HasConversion<string>().HasMaxLength(10);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<VocabularyItem>().WithMany().HasForeignKey(x => x.VocabularyItemId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.UserId, x.CreatedAtUtc });
    }
}

public sealed class UserAchievementConfiguration : IEntityTypeConfiguration<UserAchievement>
{
    public void Configure(EntityTypeBuilder<UserAchievement> b)
    {
        b.ToTable("UserAchievements");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(50).IsRequired();
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.UserId, x.Code }).IsUnique();
    }
}