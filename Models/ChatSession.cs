using OpenAI.Chat;

namespace Ayurveda_chatBot.Models
{
    public class ChatSession : BaseEntity
    {
        public Guid UserId { get; set; }
        public User User { get; set; }

        public string SessionName { get; set; } = string.Empty;
    }
}
