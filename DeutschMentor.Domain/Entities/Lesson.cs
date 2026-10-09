namespace DeutschMentor.Domain.Entities
{
    public class Lesson : BaseEntity
    {
        public Guid UnitId { get; set; }
        public Unit? Unit { get; set; }
        public int Order { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string TitleEn { get; set; } = string.Empty;
        public string TitleAr { get; set; } = string.Empty;
        public int EstimatedMinutes { get; set; } = 10;
        public ContentStatus Status { get; set; } = ContentStatus.Draft;
    }
}
