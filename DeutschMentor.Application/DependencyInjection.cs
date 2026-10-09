namespace DeutschMentor.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ILearningService, LearningService>();
        services.AddScoped<IVocabularyService, VocabularyService>();
        services.AddScoped<IGrammarService, GrammarService>();
        return services;
    }
}