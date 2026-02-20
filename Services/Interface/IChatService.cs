using Ayurveda_chatBot.DTO;
using Ayurveda_chatBot.Models;

namespace Ayurveda_chatBot.Services.Interface
{
    public interface IChatService
    {
        Task<Guid> CreateSessionAsync(Guid userId, string sessionName);
        Task ProcessMessageStream(Guid userId, SendMessageDto dto, HttpResponse response);
        Task<List<ChatSession>> GetUserSessionsAsync(Guid userId);
        Task<List<ChatHistoryDto>> GetUserHistoryAsync(Guid sessionId);
        Task<string> DeleteChatAsync(Guid chatId);
        Task<string> DeleteSessionAsync(Guid sessionId);
    }
}
