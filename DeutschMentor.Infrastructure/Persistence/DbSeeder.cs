namespace DeutschMentor.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task InitializeAsync(IServiceProvider services, bool applyMigrations)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (applyMigrations)
        {
            await db.Database.MigrateAsync();
        }

        await ContentSeeder.EnsureLevelsAsync(db);

        if (applyMigrations)
        {
            await ContentSeeder.SeedSampleContentAsync(db);
            await PracticeSeeder.SeedAsync(db);
        }

        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var email = config["Seed:AdminEmail"]?.Trim().ToLowerInvariant();
        var password = config["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        if (await db.Users.AnyAsync(u => u.Email == email))
        {
            return;
        }

        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var clock = scope.ServiceProvider.GetRequiredService<TimeProvider>();

        db.Users.Add(new User
        {
            FullName = "Administrator",
            Email = email,
            PasswordHash = hasher.Hash(password),
            Role = UserRole.Admin,
            EmailConfirmedAtUtc = clock.GetUtcNow().UtcDateTime
        });

        await db.SaveChangesAsync();
    }
}