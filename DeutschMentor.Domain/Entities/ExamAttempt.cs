namespace DeutschMentor.Domain.Entities
{
    public class ExamAttempt : BaseEntity
    {
        public Guid UserId { get; set; }
        public ExamKind Kind { get; set; }
        public Guid? LevelId { get; set; }
        public Guid? PlacedLevelId { get; set; }
        public ExamAttemptStatus Status { get; set; } = ExamAttemptStatus.InProgress;
        public DateTime StartedAtUtc { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
        public DateTime? SubmittedAtUtc { get; set; }
        public string QuestionIdsJson { get; set; } = "[]";
        public int TotalQuestions { get; set; }
        public int CorrectCount { get; set; }
        public int ScorePercent { get; set; }
        public bool Passed { get; set; }
        public string SkillScoresJson { get; set; } = "[]";
    }

}
