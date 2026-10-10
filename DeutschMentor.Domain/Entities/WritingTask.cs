namespace DeutschMentor.Domain.Entities
{
    public class WritingTask : BaseEntity
    {
        public Guid LessonId { get; set; }
        public Lesson? Lesson { get; set; }
        public int Order { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Prompt { get; set; } = string.Empty;
        public string SampleAnswer { get; set; } = string.Empty;
        public int MinWords { get; set; } = 10;
        public int PassPercent { get; set; } = 60;
        public string RulesJson { get; set; } = "{}";
    }
}
