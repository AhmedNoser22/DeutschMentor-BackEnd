namespace DeutschMentor.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<PendingRegistration> PendingRegistrations { get; }
    DbSet<PasswordResetCode> PasswordResetCodes { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}