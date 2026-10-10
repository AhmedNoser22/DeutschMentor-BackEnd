namespace DeutschMentor.Domain.Entities
{
    public class ExerciseAttempt : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid ExerciseId { get; set; }
        public string AnswerJson { get; set; } = "[]";
        public bool IsCorrect { get; set; }
    }
}
