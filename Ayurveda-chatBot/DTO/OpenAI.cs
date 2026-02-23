namespace Ayurveda_chatBot.DTO
{
    public class OpenAIResponseDto
    {
        public List<Choice> Choices { get; set; }
    }

    public class Choice
    {
        public Message Message { get; set; }
    }

    public class Message
    {
        public string Role { get; set; }
        public string Content { get; set; }
    }

    public class OpenAIStreamResponseDto
    {
        public List<StreamChoice> Choices { get; set; }
    }

    public class StreamChoice
    {
        public StreamDelta Delta { get; set; }
    }

    public class StreamDelta
    {
        public string Content { get; set; }
    }
}
