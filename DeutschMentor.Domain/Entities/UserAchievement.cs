namespace DeutschMentor.Domain.Entities
{
    public class UserAchievement : BaseEntity
    {
        public Guid UserId { get; set; }
        public string Code { get; set; } = string.Empty;
        public DateTime UnlockedAtUtc { get; set; }
    }
}
