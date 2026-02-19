namespace Ayurveda_chatBot.Models
{
    public class User : BaseEntity
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public int? Age { get; set; } = 0;
        public string? Gender { get; set; }
        public string? Diet { get; set; }
        public string? Weight { get; set; }
    }
}
