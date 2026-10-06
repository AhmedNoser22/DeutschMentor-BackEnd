namespace DeutschMentor.Application.Authentication;

public sealed class AccountService(
    IApplicationDbContext db,
    IPasswordHasher passwordHasher,
    ITokenService tokens,
    TimeProvider clock) : IAccountService
{
    public async Task<UserDto> GetMeAsync(Guid userId, CancellationToken ct) =>
        UserMapper.ToDto(await LoadUserAsync(userId, ct));

    public async Task<UserDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct)
    {
        var user = await LoadUserAsync(userId, ct);
        user.FullName = AuthValidator.NormalizeName(request.FullName);
        await db.SaveChangesAsync(ct);
        return UserMapper.ToDto(user);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct)
    {
        var user = await LoadUserAsync(userId, ct);
        AuthValidator.EnsureValidPassword(request.NewPassword);

        if (user.PasswordHash is not null
            && !passwordHasher.Verify(request.CurrentPassword ?? string.Empty, user.PasswordHash))
        {
            throw new BadRequestException("wrong_password", "Your current password is not correct.");
        }

        user.PasswordHash = passwordHasher.Hash(request.NewPassword!);

        var keepHash = string.IsNullOrEmpty(request.CurrentRefreshToken)
            ? null
            : tokens.HashRefreshToken(request.CurrentRefreshToken);

        await db.RevokeRefreshTokensAsync(user.Id, clock.GetUtcNow().UtcDateTime, keepHash, ct);
        await db.SaveChangesAsync(ct);
    }

    private async Task<User> LoadUserAsync(Guid userId, CancellationToken ct) =>
        await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
        ?? throw new NotFoundException("user_not_found", "User not found.");
}