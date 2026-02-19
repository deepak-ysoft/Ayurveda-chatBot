using Ayurveda_chatBot.Data;
using Ayurveda_chatBot.DTO;
using Ayurveda_chatBot.Helpers;
using Ayurveda_chatBot.Models;
using Ayurveda_chatBot.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace Ayurveda_chatBot.Services.Implementations
{
    public class ChatService : IChatService
    {
        private readonly IOpenAIService _openAIService;
        private readonly ApplicationDbContext _context;

        public ChatService(IOpenAIService openAIService, ApplicationDbContext context)
        {
            _openAIService = openAIService;
            _context = context;
        }

        public async Task<ChatResponseDto> ProcessMessage(Guid userId, string message)
        {
            if (EmergencyChecker.IsEmergency(message))
            {
                return new ChatResponseDto
                {
                    Answer = "⚠️ Please seek immediate medical attention.",
                    Disclaimer = null
                };
            }

            var aiResponse = await _openAIService.GetResponseAsync(message);

            var chat = new ChatHistory
            {
                UserId = userId,
                UserQuestion = message,
                BotResponse = aiResponse
            };

            _context.ChatHistories.Add(chat);
            await _context.SaveChangesAsync();

            return new ChatResponseDto
            {
                Answer = aiResponse,
                Disclaimer = "This assistant provides educational Ayurvedic wellness information only. It does not replace professional medical consultation."
            };
        }

        public async Task<List<ChatHistoryDto>> GetUserHistoryAsync(Guid userId)
        {
            return await _context.ChatHistories
                .Where(x => x.UserId == userId && !x.IsDeleted)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new ChatHistoryDto
                {
                    Question = x.UserQuestion,
                    Answer = x.BotResponse,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();
        }
    }
}
