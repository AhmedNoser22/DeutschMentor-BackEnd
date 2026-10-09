namespace DeutschMentor.Domain.Entities
{
    public class GrammarTopic : BaseEntity
    {
        public Guid LessonId { get; set; }
        public Lesson? Lesson { get; set; }
        public int Order { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string ContentJson { get; set; } = "{}";
    }
}
