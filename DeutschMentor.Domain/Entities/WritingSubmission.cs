namespace DeutschMentor.Domain.Entities
{
    public class WritingSubmission : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid WritingTaskId { get; set; }
        public string Text { get; set; } = string.Empty;
        public int WordCount { get; set; }
        public int ScorePercent { get; set; }
        public bool Passed { get; set; }
        public string FeedbackJson { get; set; } = "[]";
        public WritingReviewStatus ReviewStatus { get; set; } = WritingReviewStatus.NotRequested;
        public string? TeacherFeedback { get; set; }
        public int? TeacherScorePercent { get; set; }
        public DateTime? ReviewedAtUtc { get; set; }
        public Guid? ReviewedByUserId { get; set; }
    }
}
