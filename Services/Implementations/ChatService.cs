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
            // Emergency check (rule-based safety)
            if (EmergencyChecker.IsEmergency(message))
            {
                return new ChatResponseDto
                {
                    Answer = "⚠️ Please seek immediate medical attention.",
                    Disclaimer = null
                };
            }

            // Get user
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Id == userId && !x.IsDeleted);

            if (user == null)
                throw new Exception("User not found.");

            // Get user dosha
            var userDosha = await _context.UserSavedDoshas
                .Include(x => x.Dosha)
                .FirstOrDefaultAsync(x => x.UserId == userId && !x.IsDeleted);

            if (userDosha == null)
                throw new Exception("User has not completed onboarding.");

            var previousChats = await _context.ChatHistories
      .Where(x => x.UserId == userId && !x.IsDeleted)
      .OrderByDescending(x => x.CreatedAt)
      .Take(3)
      .OrderBy(x => x.CreatedAt)
      .ToListAsync();

            // Call AI with context
            var aiResponse = await _openAIService.GetResponseAsync(
                message,
                user,
                userDosha.Dosha, previousChats);

            // Save chat history
            var chat = new ChatHistory
            {
                UserId = userId,
                UserQuestion = message,
                BotResponse = aiResponse
            };

            _context.ChatHistories.Add(chat);
            await _context.SaveChangesAsync();

            // Return response
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
                .OrderBy(x => x.CreatedAt)
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
