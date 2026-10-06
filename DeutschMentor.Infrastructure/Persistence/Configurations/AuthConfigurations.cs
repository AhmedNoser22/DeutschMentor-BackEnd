namespace DeutschMentor.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("Users");
        b.HasKey(x => x.Id);
        b.Property(x => x.FullName).HasMaxLength(100).IsRequired();
        b.Property(x => x.Email).HasMaxLength(254).IsRequired();
        b.Property(x => x.PasswordHash).HasMaxLength(200);
        b.Property(x => x.GoogleSubject).HasMaxLength(100);
        b.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.CurrentLevel).HasConversion<string>().HasMaxLength(5);
        b.Property(x => x.StartPreference).HasConversion<string>().HasMaxLength(20);
        b.HasIndex(x => x.Email).IsUnique();
        b.HasIndex(x => x.GoogleSubject).IsUnique().HasFilter("[GoogleSubject] IS NOT NULL");
    }
}

public sealed class PendingRegistrationConfiguration : IEntityTypeConfiguration<PendingRegistration>
{
    public void Configure(EntityTypeBuilder<PendingRegistration> b)
    {
        b.ToTable("PendingRegistrations");
        b.HasKey(x => x.Id);
        b.Property(x => x.FullName).HasMaxLength(100).IsRequired();
        b.Property(x => x.Email).HasMaxLength(254).IsRequired();
        b.Property(x => x.PasswordHash).HasMaxLength(200).IsRequired();
        b.Property(x => x.CodeHash).HasMaxLength(100).IsRequired();
        b.Property(x => x.StartPreference).HasConversion<string>().HasMaxLength(20);
        b.HasIndex(x => x.Email).IsUnique();
    }
}

public sealed class PasswordResetCodeConfiguration : IEntityTypeConfiguration<PasswordResetCode>
{
    public void Configure(EntityTypeBuilder<PasswordResetCode> b)
    {
        b.ToTable("PasswordResetCodes");
        b.HasKey(x => x.Id);
        b.Property(x => x.CodeHash).HasMaxLength(100).IsRequired();
        b.HasIndex(x => x.UserId).IsUnique();
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("RefreshTokens");
        b.HasKey(x => x.Id);
        b.Property(x => x.TokenHash).HasMaxLength(100).IsRequired();
        b.HasIndex(x => x.TokenHash).IsUnique();
        b.HasIndex(x => x.UserId);
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}