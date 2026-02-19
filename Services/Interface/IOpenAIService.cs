using Ayurveda_chatBot.Models;

namespace Ayurveda_chatBot.Services.Interface
{
    public interface IOpenAIService
    {
        Task<string> GetResponseAsync(
            string userMessage,
            User user,
            Dosha dosha);
    }
}
