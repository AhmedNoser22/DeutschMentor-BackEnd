namespace DeutschMentor.Application.Authentication.Interfaces;

public sealed record AccessToken(string Value, DateTime ExpiresAtUtc);

public sealed record GoogleUserInfo(string Subject, string Email, string? FullName, bool EmailVerified);

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface ICodeService
{
    string Generate();
    string Hash(string email, string code);
    bool Verify(string email, string code, string hash);
}

public interface ITokenService
{
    AccessToken CreateAccessToken(User user);
    string CreateRefreshToken();
    string HashRefreshToken(string rawToken);
}

public interface IGoogleTokenValidator
{
    Task<GoogleUserInfo> ValidateAsync(string idToken, CancellationToken ct);
}