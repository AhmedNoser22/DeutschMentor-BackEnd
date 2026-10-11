namespace DeutschMentor.Infrastructure.Persistence.Configurations;

public sealed class LevelConfiguration : IEntityTypeConfiguration<Level>
{
    public void Configure(EntityTypeBuilder<Level> b)
    {
        b.ToTable("Levels");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasConversion<string>().HasMaxLength(5);
        b.Property(x => x.Title).HasMaxLength(50).IsRequired();
        b.Property(x => x.Description).HasMaxLength(200).IsRequired();
        b.HasIndex(x => x.Code).IsUnique();
        b.HasIndex(x => x.Order).IsUnique();
    }
}

public sealed class ModuleConfiguration : IEntityTypeConfiguration<Module>
{
    public void Configure(EntityTypeBuilder<Module> b)
    {
        b.ToTable("Modules");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(100).IsRequired();
        b.Property(x => x.TitleEn).HasMaxLength(100).IsRequired();
        b.HasOne(x => x.Level).WithMany().HasForeignKey(x => x.LevelId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.LevelId, x.Order }).IsUnique();
    }
}

public sealed class UnitConfiguration : IEntityTypeConfiguration<Domain.Entities.Unit>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Unit> b)
    {
        b.ToTable("Units");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(100).IsRequired();
        b.Property(x => x.TitleEn).HasMaxLength(100).IsRequired();
        b.HasOne(x => x.Module).WithMany().HasForeignKey(x => x.ModuleId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.ModuleId, x.Order }).IsUnique();
    }
}

public sealed class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> b)
    {
        b.ToTable("Lessons");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(20).IsRequired();
        b.Property(x => x.Title).HasMaxLength(100).IsRequired();
        b.Property(x => x.TitleEn).HasMaxLength(100).IsRequired();
        b.Property(x => x.TitleAr).HasMaxLength(100).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.HasOne(x => x.Unit).WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.Code).IsUnique();
        b.HasIndex(x => new { x.UnitId, x.Order }).IsUnique();
    }
}