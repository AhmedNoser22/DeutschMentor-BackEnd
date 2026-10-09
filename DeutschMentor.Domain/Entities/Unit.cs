namespace DeutschMentor.Domain.Entities
{
    public class Unit : BaseEntity
    {
        public Guid ModuleId { get; set; }
        public Module? Module { get; set; }
        public int Order { get; set; }
        public string Title { get; set; } = string.Empty;
        public string TitleEn { get; set; } = string.Empty;
    }
}
