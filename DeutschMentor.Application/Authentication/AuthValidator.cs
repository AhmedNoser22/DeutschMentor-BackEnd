namespace DeutschMentor.Application.Authentication;

internal static class AuthValidator
{
    public static string NormalizeEmail(string? value)
    {
        var email = (value ?? string.Empty).Trim().ToLowerInvariant();
        if (email.Length is 0 or > 254 || !MailAddress.TryCreate(email, out var parsed) || parsed.Address != email)
        {
            throw new BadRequestException("invalid_email", "Enter a valid email address.");
        }

        return email;
    }

    public static string NormalizeName(string? value)
    {
        var name = (value ?? string.Empty).Trim();
        if (name.Length is < 2 or > 100)
        {
            throw new BadRequestException("invalid_name", "Enter your full name (2 to 100 characters).");
        }

        return name;
    }

    public static string FallbackName(string? name, string email)
    {
        var value = string.IsNullOrWhiteSpace(name) ? email.Split('@')[0] : name.Trim();
        return value.Length > 100 ? value[..100] : value;
    }

    public static void EnsureValidPassword(string? password)
    {
        if (string.IsNullOrEmpty(password)
            || password.Length is < 8 or > 128
            || !password.Any(char.IsLetter)
            || !password.Any(char.IsDigit))
        {
            throw new BadRequestException("weak_password", "Use 8 to 128 characters with at least one letter and one number.");
        }
    }
}