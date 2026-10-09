namespace DeutschMentor.Infrastructure.Persistence.Configurations;

public sealed class VocabularyItemConfiguration : IEntityTypeConfiguration<VocabularyItem>
{
    public void Configure(EntityTypeBuilder<VocabularyItem> b)
    {
        b.ToTable("VocabularyItems");
        b.HasKey(x => x.Id);
        b.Property(x => x.German).HasMaxLength(100).IsRequired();
        b.Property(x => x.Article).HasConversion<string>().HasMaxLength(10);
        b.Property(x => x.PartOfSpeech).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.English).HasMaxLength(150).IsRequired();
        b.Property(x => x.Arabic).HasMaxLength(150).IsRequired();
        b.Property(x => x.ExampleGerman).HasMaxLength(300).IsRequired();
        b.Property(x => x.ExampleEnglish).HasMaxLength(300).IsRequired();
        b.HasOne(x => x.Lesson).WithMany().HasForeignKey(x => x.LessonId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.LessonId, x.Order }).IsUnique();
    }
}

public sealed class GrammarTopicConfiguration : IEntityTypeConfiguration<GrammarTopic>
{
    public void Configure(EntityTypeBuilder<GrammarTopic> b)
    {
        b.ToTable("GrammarTopics");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(100).IsRequired();
        b.Property(x => x.Summary).HasMaxLength(300).IsRequired();
        b.Property(x => x.ContentJson).IsRequired();
        b.HasOne(x => x.Lesson).WithMany().HasForeignKey(x => x.LessonId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.LessonId, x.Order }).IsUnique();
    }
}