using Ayurveda_chatBot.DTO;

namespace Ayurveda_chatBot.Services.Interface
{
    public interface IChatService
    {
        Task<ChatResponseDto> ProcessMessage(Guid userId, string message);
    }
}
