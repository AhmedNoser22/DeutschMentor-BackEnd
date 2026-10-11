namespace DeutschMentor.Application.Certificates.Dtos;

public enum CertificateSlotStatus
{
    Issued,
    NotPassed,
    PlacementOnly,
    Locked
}

public sealed record CertificateDto(
    Guid Id,
    string Code,
    string LevelCode,
    string LevelTitle,
    string GermanLevelName,
    string RecipientName,
    int ScorePercent,
    DateTime IssuedAtUtc);

public sealed record CertificateSlotDto(
    string LevelCode,
    string LevelTitle,
    CertificateSlotStatus Status,
    CertificateDto? Certificate);

public sealed record VerifyCertificateDto(
    bool Valid,
    string? Code,
    string? RecipientName,
    string? LevelCode,
    string? LevelTitle,
    string? GermanLevelName,
    int? ScorePercent,
    DateTime? IssuedAtUtc)
{
    public static VerifyCertificateDto Invalid { get; } = new(false, null, null, null, null, null, null, null);
}