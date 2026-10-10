namespace DeutschMentor.Domain.Entities
{
    public class Exercise : BaseEntity
    {
        public Guid LessonId { get; set; }
        public Lesson? Lesson { get; set; }
        public Guid? ReadingPassageId { get; set; }
        public ReadingPassage? ReadingPassage { get; set; }
        public int Order { get; set; }
        public ExerciseType Type { get; set; }
        public string Prompt { get; set; } = string.Empty;
        public string PayloadJson { get; set; } = "{}";
        public string ExplanationEn { get; set; } = string.Empty;
        public string ExplanationAr { get; set; } = string.Empty;
    }
}
