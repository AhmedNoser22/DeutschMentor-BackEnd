namespace DeutschMentor.Domain.Entities
{
    public class VocabularyItem : BaseEntity
    {
        public Guid LessonId { get; set; }
        public Lesson? Lesson { get; set; }
        public int Order { get; set; }
        public string German { get; set; } = string.Empty;
        public Article? Article { get; set; }
        public PartOfSpeech PartOfSpeech { get; set; } = PartOfSpeech.Noun;
        public string English { get; set; } = string.Empty;
        public string Arabic { get; set; } = string.Empty;
        public string ExampleGerman { get; set; } = string.Empty;
        public string ExampleEnglish { get; set; } = string.Empty;
    }
}
