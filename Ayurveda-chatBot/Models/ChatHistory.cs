namespace Ayurveda_chatBot.Models
{
    public class ChatHistory : BaseEntity
    {
        public Guid ChatSessionId { get; set; }
        public ChatSession Session { get; set; }

        public string UserQuestion { get; set; } = string.Empty;
        public string BotResponse { get; set; } = string.Empty;
    }
}
