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

        public async Task<Guid> CreateSessionAsync(Guid userId, string sessionName)
        {
            var session = new ChatSession
            {
                UserId = userId,
                SessionName = sessionName
            };

            _context.ChatSessions.Add(session);
            await _context.SaveChangesAsync();

            return session.Id;
        }

        public async Task<ChatResponseDto> ProcessMessage(Guid userId, SendMessageDto dto)
        {
            // Emergency check (rule-based safety)
            if (EmergencyChecker.IsEmergency(dto.message))
            {
                return new ChatResponseDto
                {
                    Answer = "⚠️ Please seek immediate medical attention.",
                    Disclaimer = null
                };
            }

            Guid sessionId = dto.ChatSessionId ?? Guid.Empty;

            if (dto.ChatSessionId == Guid.Empty)
            {
                // create new session
                var session = new ChatSession()
                {
                    UserId = userId,
                    SessionName = GenerateSessionTitle(dto.message)
                };

                await _context.ChatSessions.AddAsync(session);
                await _context.SaveChangesAsync();
                sessionId = session.Id;
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
             .Include(x => x.Session)
             .Where(x => x.Session.UserId == userId && !x.IsDeleted)
             .OrderByDescending(x => x.CreatedAt)
             .Take(3)
             .OrderBy(x => x.CreatedAt)
             .ToListAsync();

            // Call AI with context
            var aiResponse = await _openAIService.GetResponseAsync(
                dto.message,
                user,
                userDosha.Dosha, previousChats);

            // Save chat history
            var chat = new ChatHistory
            {
                ChatSessionId = sessionId,
                UserQuestion = dto.message,
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

        private string GenerateSessionTitle(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return "New Chat";

            message = message.Trim();

            // Remove line breaks
            message = message.Replace("\n", " ").Replace("\r", "");

            // Limit to 20 characters
            if (message.Length > 20)
                message = message.Substring(0, 20) + "...";

            return message;
        }

        public async Task<List<ChatSession>> GetUserSessionsAsync(Guid userId)
        {
            return await _context.ChatSessions
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<ChatHistoryDto>> GetUserHistoryAsync(Guid sessionId)
        {
            return await _context.ChatHistories
                .Where(x => x.ChatSessionId == sessionId && !x.IsDeleted)
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
