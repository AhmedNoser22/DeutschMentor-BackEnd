namespace DeutschMentor.Domain.Entities;

public class Certificate : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid LevelId { get; set; }
    public Guid ExamAttemptId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public int ScorePercent { get; set; }
    public DateTime IssuedAtUtc { get; set; }
}