namespace DeutschMentor.Application.Authentication;

internal static class RefreshTokenExtensions
{
    public static async Task RevokeRefreshTokensAsync(
        this IApplicationDbContext db,
        Guid userId,
        DateTime nowUtc,
        string? exceptTokenHash,
        CancellationToken ct)
    {
        var active = await db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null && t.TokenHash != exceptTokenHash)
            .ToListAsync(ct);

        foreach (var token in active)
        {
            token.RevokedAtUtc = nowUtc;
        }
    }
}