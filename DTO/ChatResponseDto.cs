namespace Ayurveda_chatBot.DTO
{
    public class ChatResponseDto
    {
        public string Answer { get; set; }
        public string Disclaimer { get; set; }
    }

    public class ChatRequestDto
    {
        public string Message { get; set; }
    }
    public class ChatHistoryDto
    {
        public string Question { get; set; }
        public string Answer { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
