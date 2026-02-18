namespace Ayurveda_chatBot.Models
{
    public class UserSavedHerb : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid HerbId { get; set; }

        public User User { get; set; }
        public Herb Herb { get; set; }
    }

}
