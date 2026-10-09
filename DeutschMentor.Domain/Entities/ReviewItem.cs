namespace DeutschMentor.Domain.Entities
{
    public class ReviewItem : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid VocabularyItemId { get; set; }
        public DateTime DueAtUtc { get; set; }
        public int IntervalDays { get; set; }
        public int Repetitions { get; set; }
        public double EaseFactor { get; set; } = 2.5;
    }
}
