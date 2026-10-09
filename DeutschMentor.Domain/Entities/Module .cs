namespace DeutschMentor.Domain.Entities
{
    public class Module : BaseEntity
    {
        public Guid LevelId { get; set; }
        public Level? Level { get; set; }
        public int Order { get; set; }
        public string Title { get; set; } = string.Empty;
        public string TitleEn { get; set; } = string.Empty;
    }
}
