using Ayurveda_chatBot.Data;
using Ayurveda_chatBot.DTO;
using Ayurveda_chatBot.Helpers;
using Ayurveda_chatBot.Models;
using Ayurveda_chatBot.Services.Interface;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;

namespace Ayurveda_chatBot.Services.Implementations
{
    public class ChatService : IChatService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public ChatService(ApplicationDbContext context,IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<ChatResponseDto> ProcessMessage(Guid userId, SendMessageDto dto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Id == userId && !x.IsDeleted);

            if (user == null)
                throw new Exception("User not found.");

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

            string summarizedContext = previousChats.Any()
                ? string.Join("\n\n",
                    previousChats.Select(x =>
                        $"User: {x.UserQuestion}\nAI: {x.BotResponse}"))
                : "";

            Guid sessionId;

            if (dto.ChatSessionId == null || dto.ChatSessionId == Guid.Empty)
            {
                var newSession = new ChatSession
                {
                    UserId = userId,
                    SessionName = GenerateSessionTitle(dto.message)
                };

                _context.ChatSessions.Add(newSession);
                await _context.SaveChangesAsync();

                sessionId = newSession.Id;
            }
            else
            {
                sessionId = dto.ChatSessionId.Value;
            }

            var systemPrompt = BuildSystemPrompt(user, userDosha, summarizedContext);

            // 🔥 Call Groq Normally (NO STREAM)
            var fullResponse = await GetGroqResponse(systemPrompt, dto.message);

            var chat = new ChatHistory
            {
                ChatSessionId = sessionId,
                UserQuestion = dto.message,
                BotResponse = fullResponse
            };

            _context.ChatHistories.Add(chat);
            await _context.SaveChangesAsync();

            return new ChatResponseDto
            {
                Answer = fullResponse,
                SessionId = sessionId
            };
        }

        private async Task<string> GetGroqResponse(string systemPrompt, string userMessage)
        {
            var apiKey = _configuration["Groq:ApiKey"];
            var model = _configuration["Groq:Model"];

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);

            var requestBody = new
            {
                model = model,
                messages = new[]
                {
            new { role = "system", content = systemPrompt },
            new { role = "user", content = userMessage }
        }
            };

            var response = await client.PostAsJsonAsync(
                "https://api.groq.com/openai/v1/chat/completions",
                requestBody);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OpenAIResponseDto>();

            return result?.Choices?.FirstOrDefault()?.Message?.Content ?? "";
        }

        private string BuildSystemPrompt(
            User user,
            UserSavedDosha userDosha,
            string summarizedContext)
        {
            return $@"
                You are Veda, a wise and warm Ayurvedic wellness guide — like a knowledgeable 
                friend who makes ancient wisdom feel simple and relevant to modern life.

                ==============================
                USER PROFILE
                Age: {user.Age}
                Gender: {user.Gender}
                Diet: {user.Diet}
                Weight: {user.Weight}
                Dominant Dosha: {userDosha.Dosha.Name}
                ==============================

                CONVERSATION CONTEXT
                {(string.IsNullOrEmpty(summarizedContext)
                            ? "No previous conversation."
                            : $"Previous Summary:\n{summarizedContext}")}

                ==============================
                PERSONALITY
                - Speak like a wise, friendly mentor
                - Use simple language and short analogies
                - Be calm, grounding, and encouraging
                - Personalize advice for THIS user

                ==============================
                RESPONSE BEHAVIOR
                - Continue naturally if related to previous conversation
                - Ignore previous context if unrelated
                - Always personalize using:
                  Dosha: {userDosha.Dosha.Name}
                  Diet: {user.Diet}
                  Age: {user.Age}
                  Gender: {user.Gender}
                - Suggestions must align with {user.Diet} diet

                ==============================
                AYURVEDIC GUIDELINES
                - Recommend foods, lifestyle (Dinacharya), seasonal alignment, and gentle herbal suggestions
                - Never suggest dosages
                - Never claim cures
                - Never diagnose
                - If symptoms sound serious, say:
                  ""Please consult a qualified healthcare provider.""

                ==============================
                RESPONSE FORMAT (MANDATORY)

                Hook line

                - Point 1 (one sentence)
                - Point 2 (one sentence)
                - Point 3 (one sentence)
                - Optional Point 4 (one sentence)

                Short warm closing line.

                ⚠️ Educational only. Consult a doctor for medical concerns.

                ==============================
                STRICT RULES
                - Use real newline characters
                - Keep response short and accurate
                - Each point must be one sentence only
                - Do not combine points into paragraphs
                - No filler openings
                - Keep tone warm and modern
                ";
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
                .Where(x => x.UserId == userId && !x.IsDeleted )
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
                    Id = x.Id,
                    Question = x.UserQuestion,
                    Answer = x.BotResponse,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<string> DeleteChatAsync(Guid chatId)
        {
            var chat = await _context.ChatHistories.FirstOrDefaultAsync(x => x.Id == chatId);

            _context.ChatHistories.Remove(chat);
            int n = await _context.SaveChangesAsync();

            if (n > 0) 
                return "Chat deleted.";

            return "Failed to deleted chat";
        }

        public async Task<string> DeleteSessionAsync(Guid sessionId)
        {
            var session = await _context.ChatSessions.FirstOrDefaultAsync(x => x.Id == sessionId);

            _context.ChatSessions.Remove(session);
            int n = await _context.SaveChangesAsync();

            if (n > 0)
                return "Session deleted.";

            return "Failed to deleted session";
        }
    }
}
