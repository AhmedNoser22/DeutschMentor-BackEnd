namespace DeutschMentor.Application.Certificates;

internal static class CertificateIssuer
{
    private const string Alphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";

    public static Certificate Create(User user, Level level, Guid examAttemptId, int scorePercent, DateTime issuedAtUtc) => new()
    {
        UserId = user.Id,
        LevelId = level.Id,
        ExamAttemptId = examAttemptId,
        RecipientName = user.FullName,
        ScorePercent = scorePercent,
        IssuedAtUtc = issuedAtUtc,
        Code = NewCode(level.Code, issuedAtUtc.Year)
    };

    public static string Normalize(string? value) => (value ?? string.Empty).Trim().ToUpperInvariant();

    private static string NewCode(CefrLevel level, int year)
    {
        var chars = new char[8];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return $"DM-{level}-{year}-{new string(chars)}";
    }
}