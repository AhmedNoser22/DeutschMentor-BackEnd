namespace DeutschMentor.Domain.Entities
{
    public class LevelPass : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid LevelId { get; set; }
        public int ScorePercent { get; set; }
        public DateTime PassedAtUtc { get; set; }
        public LevelPassSource Source { get; set; } = LevelPassSource.Exam;
    }
}
