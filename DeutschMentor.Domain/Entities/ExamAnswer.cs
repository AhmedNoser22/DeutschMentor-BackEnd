namespace DeutschMentor.Domain.Entities
{
    public class ExamAnswer : BaseEntity
    {
        public Guid AttemptId { get; set; }
        public Guid QuestionId { get; set; }
        public string AnswerJson { get; set; } = "[]";
        public bool IsFlagged { get; set; }
        public bool? IsCorrect { get; set; }
    }
}
