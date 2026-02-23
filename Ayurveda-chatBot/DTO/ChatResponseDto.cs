namespace Ayurveda_chatBot.DTO
{
    public class ChatResponseDto
    {
        public Guid SessionId { get; set; }
        public string Answer { get; set; }
    }

    public class ChatHistoryDto
    {
        public Guid Id { get; set; }
        public string Question { get; set; }
        public string Answer { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SendMessageDto
    {
        public Guid? ChatSessionId { get; set; }
        public string message { get; set; } = string.Empty;
    }
}
