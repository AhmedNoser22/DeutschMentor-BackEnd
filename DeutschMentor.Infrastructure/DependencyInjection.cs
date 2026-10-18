namespace DeutschMentor.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddDbContext<ApplicationDbContext>(o =>
            o.UseSqlServer(config.GetConnectionString("Default")));
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.Configure<JwtOptions>(config.GetSection("Jwt"));
        services.Configure<SecurityOptions>(config.GetSection("Security"));
        services.Configure<GoogleOptions>(config.GetSection("Google"));
        services.Configure<SmtpOptions>(config.GetSection("Smtp"));

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ICodeService, CodeService>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IGoogleTokenValidator, GoogleTokenValidator>();
        services.Configure<BrevoOptions>(config.GetSection("Brevo"));
        if (string.Equals(config["Email:Provider"], "Brevo", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IEmailSender, BrevoEmailSender>();
        }
        else
        {
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
        }
        services.AddSingleton<ICertificatePdfGenerator, CertificatePdfGenerator>();

        services.AddHostedService<AuthCleanupService>();

        return services;
    }

    public static Task InitializeInfrastructureAsync(this IServiceProvider services, bool applyMigrations) =>
        DbSeeder.InitializeAsync(services, applyMigrations);
}