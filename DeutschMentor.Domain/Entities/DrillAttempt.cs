namespace DeutschMentor.Domain.Entities
{
    public class DrillAttempt : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid VocabularyItemId { get; set; }
        public Article ChosenArticle { get; set; }
        public bool IsCorrect { get; set; }
    }
}
