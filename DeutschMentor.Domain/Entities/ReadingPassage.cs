namespace DeutschMentor.Domain.Entities
{
    public class ReadingPassage : BaseEntity
    {
        public Guid LessonId { get; set; }
        public Lesson? Lesson { get; set; }
        public int Order { get; set; }
        public string Title { get; set; } = string.Empty;
        public string BodyJson { get; set; } = "[]";
    }
}
