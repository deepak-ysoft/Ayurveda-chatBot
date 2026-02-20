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
        public async Task ProcessMessageStream(
            Guid userId,
            SendMessageDto dto,
            HttpResponse response)
        {
            // 🔹 Get User
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Id == userId && !x.IsDeleted);

            if (user == null)
                throw new Exception("User not found.");

            // 🔹 Get Dosha
            var userDosha = await _context.UserSavedDoshas
                .Include(x => x.Dosha)
                .FirstOrDefaultAsync(x => x.UserId == userId && !x.IsDeleted);

            if (userDosha == null)
                throw new Exception("User has not completed onboarding.");

            // 🔹 Get Previous Chats
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

            // 🔹 Create or Get Session FIRST ✅
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

            // ✅ Add SessionId to Header BEFORE streaming
            response.Headers.Add("X-Session-Id", sessionId.ToString());

            // 🔥 System Prompt
            var systemPrompt = $@"
You are Veda, a wise and warm Ayurvedic wellness guide — like a knowledgeable 
friend who makes ancient wisdom feel exciting and relevant to modern life.

==============================
## USER PROFILE
Age: {user.Age}
Gender: {user.Gender}
Diet: {user.Diet}
Weight: {user.Weight}
Dominant Dosha: {userDosha.Dosha.Name}
==============================

## CONVERSATION CONTEXT
{(string.IsNullOrEmpty(summarizedContext)
          ? "No previous conversation."
          : $"Previous Summary:\n{summarizedContext}")}

==============================
## YOUR PERSONALITY
- Speak like a wise, friendly mentor — never like a medical textbook
- Use relatable comparisons (e.g., ""Think of Vata like wind — creative but easily scattered"")
- Occasionally reference nature, seasons, rhythms, or daily life
- Be encouraging, calm, and grounding
- Make the advice feel personal and tailored to THIS user
- Use simple language and explain Ayurvedic terms with short analogies
==============================

## RESPONSE BEHAVIOR
- If the current question relates to previous conversation, continue naturally
- If unrelated, ignore previous summary and start fresh
- Always personalize using:
  - Dosha: {userDosha.Dosha.Name}
  - Diet: {user.Diet}
  - Age: {user.Age}
  - Gender: {user.Gender}
- Suggestions must align with {user.Diet} diet
- Consider age and gender appropriately
==============================

## AYURVEDIC GUIDELINES
- Recommend foods, lifestyle (Dinacharya), seasonal alignment, and gentle herbal suggestions
- NEVER suggest dosages
- NEVER claim cures
- NEVER diagnose
- If symptoms sound serious, say:
  ""Please consult a qualified healthcare provider.""
==============================

## RESPONSE FORMAT (MANDATORY STRUCTURE)

You MUST follow this exact structure:

Hook line
\n
🌿 Bullet point 1
\n
🔥 Bullet point 2
\n
🌙 Bullet point 3
\n
(Optional 4th bullet with emoji)
\n
Short warm closing line
\n
⚠️ Educational only. Consult a doctor for medical concerns.

==============================

## STRICT FORMATTING RULES
- Use REAL newline characters (\n)
- Each bullet MUST start on a NEW LINE
- Each bullet MUST begin with an emoji (🌿 🔥 🌙 💧 🌸)
- Do NOT combine bullets into paragraphs
- Do NOT output everything in one block
- Do NOT remove line breaks
- No filler openers like:
  - ""Great question!""
  - ""As an Ayurvedic assistant...""
- Keep bullets to ONE sentence each
- Keep tone warm, modern, human

Failure to follow this structure is not allowed.
";

            // 🔹 Start Streaming AFTER header is set
            var fullResponse = await StreamFromGroq(
                systemPrompt,
                dto.message,
                response);

            // 🔹 Save Chat After Stream Complete
            var chat = new ChatHistory
            {
                ChatSessionId = sessionId,
                UserQuestion = dto.message,
                BotResponse = fullResponse
            };

            _context.ChatHistories.Add(chat);
            await _context.SaveChangesAsync();
        }
        private async Task<string> StreamFromGroq(
            string systemPrompt,
            string userMessage,
            HttpResponse response)
        {
            var apiKey = _configuration["Groq:ApiKey"];
            var model = _configuration["Groq:Model"];

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);

            var requestBody = new
            {
                model = model,
                stream = true,
                messages = new[]
                {
            new { role = "system", content = systemPrompt },
            new { role = "user", content = userMessage }
        }
            };

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.groq.com/openai/v1/chat/completions");

            request.Content = JsonContent.Create(requestBody);

            var apiResponse = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead);

            using var stream = await apiResponse.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            string fullResponse = "";

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (!line.StartsWith("data: ")) continue;

                var json = line.Substring(6);

                if (json == "[DONE]")
                {
                    break;
                }

                try
                {
                    var parsed = JsonSerializer.Deserialize<OpenAIStreamResponseDto>(
                        json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                    var content = parsed?.Choices?
                        .FirstOrDefault()?
                        .Delta?
                        .Content;

                    if (!string.IsNullOrEmpty(content))
                    {
                        fullResponse += content;

                        // Proper SSE format
                        await response.WriteAsync($"data: {content}\n\n");
                        await response.Body.FlushAsync();
                    }
                }
                catch
                {
                    // ignore
                }
            }

            // Send DONE event properly
            await response.WriteAsync("data: [DONE]\n\n");
            await response.Body.FlushAsync();

            return fullResponse;
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
