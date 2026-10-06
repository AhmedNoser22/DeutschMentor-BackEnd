namespace DeutschMentor.Infrastructure.BackgroundJobs;

public sealed class AuthCleanupService(
    IServiceScopeFactory scopeFactory,
    TimeProvider clock,
    ILogger<AuthCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        do
        {
            try
            {
                await CleanAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Auth cleanup failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task CleanAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = clock.GetUtcNow().UtcDateTime;

        await db.PendingRegistrations
            .Where(p => p.ExpiresAtUtc < now.AddHours(-24))
            .ExecuteDeleteAsync(ct);

        await db.PasswordResetCodes
            .Where(c => c.ExpiresAtUtc < now)
            .ExecuteDeleteAsync(ct);

        await db.RefreshTokens
            .Where(t => t.ExpiresAtUtc < now.AddDays(-7) || (t.RevokedAtUtc != null && t.RevokedAtUtc < now.AddDays(-7)))
            .ExecuteDeleteAsync(ct);
    }
}