namespace DeutschMentor.Domain.Entities;

public class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public string? GoogleSubject { get; set; }
    public UserRole Role { get; set; } = UserRole.Student;
    public CefrLevel CurrentLevel { get; set; } = CefrLevel.A1;
    public StartPreference StartPreference { get; set; } = StartPreference.StartAtA1;
    public DateTime EmailConfirmedAtUtc { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }
    public DateTime? PlacementTakenAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
}