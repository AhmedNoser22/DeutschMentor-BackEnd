namespace DeutschMentor.Infrastructure.Persistence.Configurations;

public sealed class ExamQuestionConfiguration : IEntityTypeConfiguration<ExamQuestion>
{
    public void Configure(EntityTypeBuilder<ExamQuestion> b)
    {
        b.ToTable("ExamQuestions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Skill).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Prompt).HasMaxLength(600).IsRequired();
        b.Property(x => x.PayloadJson).IsRequired();
        b.Property(x => x.ExplanationEn).HasMaxLength(500).IsRequired();
        b.Property(x => x.ExplanationAr).HasMaxLength(500).IsRequired();
        b.HasOne(x => x.Level).WithMany().HasForeignKey(x => x.LevelId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.LevelId, x.IsActive });
    }
}

public sealed class ExamAttemptConfiguration : IEntityTypeConfiguration<ExamAttempt>
{
    public void Configure(EntityTypeBuilder<ExamAttempt> b)
    {
        b.ToTable("ExamAttempts");
        b.HasKey(x => x.Id);
        b.Property(x => x.Kind).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.QuestionIdsJson).IsRequired();
        b.Property(x => x.SkillScoresJson).IsRequired();
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Level>().WithMany().HasForeignKey(x => x.LevelId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Level>().WithMany().HasForeignKey(x => x.PlacedLevelId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.UserId, x.Kind });
        b.HasIndex(x => new { x.UserId, x.Kind, x.LevelId })
            .IsUnique()
            .HasFilter("[Status] = 'InProgress'");
    }
}

public sealed class ExamAnswerConfiguration : IEntityTypeConfiguration<ExamAnswer>
{
    public void Configure(EntityTypeBuilder<ExamAnswer> b)
    {
        b.ToTable("ExamAnswers");
        b.HasKey(x => x.Id);
        b.Property(x => x.AnswerJson).HasMaxLength(2000).IsRequired();
        b.HasOne<ExamAttempt>().WithMany().HasForeignKey(x => x.AttemptId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<ExamQuestion>().WithMany().HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.AttemptId, x.QuestionId }).IsUnique();
    }
}

public sealed class CertificateConfiguration : IEntityTypeConfiguration<Domain.Entities.Certificate>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Certificate> b)
    {
        b.ToTable("Certificates");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(40).IsRequired();
        b.Property(x => x.RecipientName).HasMaxLength(100).IsRequired();
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Level>().WithMany().HasForeignKey(x => x.LevelId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ExamAttempt>().WithMany().HasForeignKey(x => x.ExamAttemptId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.Code).IsUnique();
        b.HasIndex(x => new { x.UserId, x.LevelId }).IsUnique();
    }
}