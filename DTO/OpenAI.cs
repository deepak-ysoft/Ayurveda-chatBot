namespace Ayurveda_chatBot.DTO
{
    public class OpenAIResponseDto
    {
        public List<ChoiceDto> Choices { get; set; }
    }

    public class ChoiceDto
    {
        public MessageDto Message { get; set; }
    }

    public class MessageDto
    {
        public string Role { get; set; }
        public string Content { get; set; }
    }
}
