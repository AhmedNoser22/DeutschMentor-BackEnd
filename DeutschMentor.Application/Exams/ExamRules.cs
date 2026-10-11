namespace DeutschMentor.Application.Exams;

internal static class ExamRules
{
    public const int LevelQuestionCount = 40;
    public const int PlacementPerLevel = 4;
    public const int PlacementPassPercent = 70;

    public static readonly TimeSpan RetakeCooldown = TimeSpan.FromHours(24);
    public static readonly TimeSpan SubmitGrace = TimeSpan.FromSeconds(60);

    public static int LevelMinutes(int questionCount) => Math.Max(10, (int)Math.Ceiling(questionCount * 1.5));

    public static int PlacementMinutes(int questionCount) => Math.Max(10, questionCount);
}