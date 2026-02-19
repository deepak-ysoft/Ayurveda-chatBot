using Ayurveda_chatBot.DTO;
using Ayurveda_chatBot.Models;

namespace Ayurveda_chatBot.Services.Interface
{
    public interface IChatService
    {
        Task<Guid> CreateSessionAsync(Guid userId, string sessionName);
        Task<ChatResponseDto> ProcessMessage(Guid userId, SendMessageDto dto);
        Task<List<ChatSession>> GetUserSessionsAsync(Guid userId);
        Task<List<ChatHistoryDto>> GetUserHistoryAsync(Guid sessionId);
    }
}
