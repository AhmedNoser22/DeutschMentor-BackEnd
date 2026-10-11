namespace DeutschMentor.Domain.Entities
{
    public class ExamQuestion : BaseEntity
    {
        public Guid LevelId { get; set; }
        public Level? Level { get; set; }
        public ExerciseSkill Skill { get; set; }
        public ExerciseType Type { get; set; }
        public string Prompt { get; set; } = string.Empty;
        public string PayloadJson { get; set; } = "{}";
        public string ExplanationEn { get; set; } = string.Empty;
        public string ExplanationAr { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}
