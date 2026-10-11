namespace DeutschMentor.Domain.Entities
{
    public class ReviewLog : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid VocabularyItemId { get; set; }
        public ReviewRating Rating { get; set; }
        public int IntervalDaysAfter { get; set; }
        public double EaseFactorAfter { get; set; }
    }
}
