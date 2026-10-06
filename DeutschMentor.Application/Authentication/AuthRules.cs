namespace DeutschMentor.Application.Authentication;

internal static class AuthRules
{
    public static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);
    public static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);
    public const int MaxCodeAttempts = 5;
}