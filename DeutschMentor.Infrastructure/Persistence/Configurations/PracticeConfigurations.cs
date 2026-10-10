namespace DeutschMentor.Infrastructure.Persistence.Configurations;

public sealed class ReadingPassageConfiguration : IEntityTypeConfiguration<ReadingPassage>
{
    public void Configure(EntityTypeBuilder<ReadingPassage> b)
    {
        b.ToTable("ReadingPassages");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(150).IsRequired();
        b.Property(x => x.BodyJson).IsRequired();
        b.HasOne(x => x.Lesson).WithMany().HasForeignKey(x => x.LessonId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.LessonId, x.Order }).IsUnique();
    }
}

public sealed class ExerciseConfiguration : IEntityTypeConfiguration<Exercise>
{
    public void Configure(EntityTypeBuilder<Exercise> b)
    {
        b.ToTable("Exercises");
        b.HasKey(x => x.Id);
        b.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Prompt).HasMaxLength(500).IsRequired();
        b.Property(x => x.PayloadJson).IsRequired();
        b.Property(x => x.ExplanationEn).HasMaxLength(500).IsRequired();
        b.Property(x => x.ExplanationAr).HasMaxLength(500).IsRequired();
        b.HasOne(x => x.Lesson).WithMany().HasForeignKey(x => x.LessonId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.ReadingPassage).WithMany().HasForeignKey(x => x.ReadingPassageId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.LessonId, x.ReadingPassageId, x.Order }).IsUnique();
    }
}

public sealed class ExerciseAttemptConfiguration : IEntityTypeConfiguration<ExerciseAttempt>
{
    public void Configure(EntityTypeBuilder<ExerciseAttempt> b)
    {
        b.ToTable("ExerciseAttempts");
        b.HasKey(x => x.Id);
        b.Property(x => x.AnswerJson).HasMaxLength(2000).IsRequired();
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Exercise>().WithMany().HasForeignKey(x => x.ExerciseId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.UserId, x.ExerciseId });
    }
}

public sealed class WritingTaskConfiguration : IEntityTypeConfiguration<WritingTask>
{
    public void Configure(EntityTypeBuilder<WritingTask> b)
    {
        b.ToTable("WritingTasks");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(100).IsRequired();
        b.Property(x => x.Prompt).HasMaxLength(600).IsRequired();
        b.Property(x => x.SampleAnswer).HasMaxLength(1000).IsRequired();
        b.Property(x => x.RulesJson).IsRequired();
        b.HasOne(x => x.Lesson).WithMany().HasForeignKey(x => x.LessonId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.LessonId, x.Order }).IsUnique();
    }
}

public sealed class WritingSubmissionConfiguration : IEntityTypeConfiguration<WritingSubmission>
{
    public void Configure(EntityTypeBuilder<WritingSubmission> b)
    {
        b.ToTable("WritingSubmissions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Text).HasMaxLength(2000).IsRequired();
        b.Property(x => x.FeedbackJson).IsRequired();
        b.Property(x => x.ReviewStatus).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.TeacherFeedback).HasMaxLength(1000);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<WritingTask>().WithMany().HasForeignKey(x => x.WritingTaskId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.UserId, x.WritingTaskId });
        b.HasIndex(x => x.ReviewStatus);
    }
}