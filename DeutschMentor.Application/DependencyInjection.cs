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
        services.AddScoped<IExerciseService, ExerciseService>();
        services.AddScoped<IReadingService, ReadingService>();
        services.AddScoped<IWritingService, WritingService>();
        services.AddScoped<IExamService, ExamService>();
        services.AddScoped<ICertificateService, CertificateService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IDrillService, DrillService>();
        services.AddScoped<IAchievementService, AchievementService>();
        services.AddScoped<IProgressService, ProgressService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IContentImportService, ContentImportService>();
        services.AddScoped<IAdminUserService, AdminUserService>();
        return services;
    }
}