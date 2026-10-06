namespace DeutschMentor.Application.Authentication.Interfaces;

public interface IAuthService
{
    Task<PendingRegistrationResponse> RegisterAsync(RegisterRequest request, CancellationToken ct);
    Task<AuthResponse> ConfirmEmailAsync(ConfirmEmailRequest request, CancellationToken ct);
    Task<PendingRegistrationResponse> ResendCodeAsync(ResendCodeRequest request, CancellationToken ct);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct);
    Task<AuthResponse> GoogleLoginAsync(GoogleLoginRequest request, CancellationToken ct);
    Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken ct);
    Task LogoutAsync(LogoutRequest request, CancellationToken ct);
    Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct);
}

public interface IAccountService
{
    Task<UserDto> GetMeAsync(Guid userId, CancellationToken ct);
    Task<UserDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct);
}