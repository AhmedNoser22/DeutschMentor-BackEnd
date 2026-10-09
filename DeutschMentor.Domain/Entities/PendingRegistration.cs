namespace DeutschMentor.Domain.Entities;

public class PendingRegistration : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public StartPreference StartPreference { get; set; } = StartPreference.StartAtA1;
    public string CodeHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime LastSentAtUtc { get; set; }
    public int FailedAttempts { get; set; }
}