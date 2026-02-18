namespace Ayurveda_chatBot.Services.Interface
{
    public interface IOpenAIService
    {
        Task<string> GetResponseAsync(string userMessage);
    }
}
