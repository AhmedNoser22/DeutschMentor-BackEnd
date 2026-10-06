namespace DeutschMentor.Infrastructure.Identity;

public sealed class GoogleTokenValidator(IOptions<GoogleOptions> options) : IGoogleTokenValidator
{
    public async Task<GoogleUserInfo> ValidateAsync(string idToken, CancellationToken ct)
    {
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [options.Value.ClientId]
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
            return new GoogleUserInfo(payload.Subject, payload.Email, payload.Name, payload.EmailVerified);
        }
        catch (InvalidJwtException)
        {
            throw new UnauthorizedException("invalid_google_token", "Google sign-in failed. Try again.");
        }
    }
}