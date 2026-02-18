namespace Ayurveda_chatBot.Models
{
    public class ChatHistory : BaseEntity
    {
        public Guid UserId { get; set; }
        public User User { get; set; }

        public string UserQuestion { get; set; }
        public string BotResponse { get; set; }
    }
}
