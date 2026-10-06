namespace DeutschMentor.Domain.Entities;

public class PasswordResetCode : BaseEntity
{
    public Guid UserId { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime LastSentAtUtc { get; set; }
    public int FailedAttempts { get; set; }
}