namespace Ayurveda_chatBot.Models
{
    public class Herb : BaseEntity
    {
        public string EnglishName { get; set; }
        public string SanskritName { get; set; }
        public string Uses { get; set; }
        public string Contraindications { get; set; }
    }

    public class UserSavedHerb : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid HerbId { get; set; }

        public User User { get; set; }
        public Herb Herb { get; set; }
    }
}
