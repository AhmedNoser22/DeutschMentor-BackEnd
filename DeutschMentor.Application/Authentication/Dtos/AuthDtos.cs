namespace DeutschMentor.Application.Authentication.Dtos;

public sealed record RegisterRequest(string? FullName, string? Email, string? Password, StartPreference StartPreference);

public sealed record ConfirmEmailRequest(string? Email, string? Code);

public sealed record ResendCodeRequest(string? Email);

public sealed record LoginRequest(string? Email, string? Password);

public sealed record GoogleLoginRequest(string? IdToken, StartPreference? StartPreference);

public sealed record RefreshRequest(string? RefreshToken);

public sealed record LogoutRequest(string? RefreshToken);

public sealed record ForgotPasswordRequest(string? Email);

public sealed record ResetPasswordRequest(string? Email, string? Code, string? NewPassword);

public sealed record UpdateProfileRequest(string? FullName);

public sealed record ChangePasswordRequest(string? CurrentPassword, string? NewPassword, string? CurrentRefreshToken);

public sealed record UserDto(
    Guid Id,
    string FullName,
    string Email,
    UserRole Role,
    CefrLevel CurrentLevel,
    StartPreference StartPreference,
    bool HasPassword,
    bool PlacementAvailable);

public sealed record AuthResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    UserDto User);

public sealed record PendingRegistrationResponse(string Email, int CodeExpiresInSeconds, int ResendCooldownSeconds);