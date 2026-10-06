namespace DeutschMentor.Infrastructure.Identity;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 15;
}

public sealed class SecurityOptions
{
    public string CodePepper { get; set; } = string.Empty;
}

public sealed class GoogleOptions
{
    public string ClientId { get; set; } = string.Empty;
}