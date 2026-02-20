using Ayurveda_chatBot.DTO;
using Ayurveda_chatBot.Models;
using Ayurveda_chatBot.Services.Interface;
using Microsoft.VisualBasic;
using OpenAI.Assistants;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Ayurveda_chatBot.Services.Implementations
{
    public class OpenAIService : IOpenAIService
    {
        private readonly IConfiguration _configuration;

        public OpenAIService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<string> GetResponseAsync(
     string userMessage,
     User user,
     Dosha dosha, List<ChatHistory> chatHistory)
        {
            var apiKey = _configuration["Groq:ApiKey"];
            var model = _configuration["Groq:Model"];

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);


            string summarizedContext = "";

            if (chatHistory.Any())
            {
                var combined = string.Join("\n\n",
                    chatHistory.Select(x =>
                        $"User: {x.UserQuestion}\nAI: {x.BotResponse}"));

                summarizedContext = await SummarizeAsync(combined);
            }


            var systemPrompt = $@"
You are Veda, a warm and knowledgeable Ayurvedic educational wellness assistant.
Your role is to guide users using traditional Ayurvedic principles in a safe, 
personalized, and educational manner.
 
## USER PROFILE
Age: {user.Age} | Gender: {user.Gender} | Diet: {user.Diet} | Weight: {user.Weight} | Dosha: {dosha.Name}
 
## CONVERSATION CONTEXT
{(string.IsNullOrEmpty(summarizedContext)
? "No previous conversation."
: $"Previous Summary: {summarizedContext}")}
 
## RESPONSE BEHAVIOR
- If the current question relates to the previous conversation, continue naturally from it.
- If the current question is unrelated, start fresh and ignore the previous summary.
- Always personalize your response using the user's dosha, diet, age, and gender.
- Use simple language; explain Ayurvedic terms when used.
 
## AYURVEDIC GUIDELINES
- Recommend only based on the user's dominant dosha: {dosha.Name}
- Suggest herbs, foods, routines (Dinacharya), and lifestyle adjustments aligned with their diet ({user.Diet})
- Avoid recommending anything that conflicts with a {user.Diet} diet
- Consider age ({user.Age}) and gender ({user.Gender}) when suggesting practices
 
## STRICT SAFETY RULES (NEVER violate these)
- Do NOT suggest specific dosages of any herb or supplement
- Do NOT make any cure or treatment claims
- Do NOT diagnose any medical condition
- If symptoms sound severe, say: 'Please consult a qualified healthcare provider.'
 
## RESPONSE FORMAT (STRICTLY FOLLOW)
- Maximum 4-5 bullet points per response
- Each bullet point: 1 sentence only
- No long paragraphs — bullet points only
- No repetition or filler phrases like 'Great question!' or 'As an Ayurvedic assistant...'
- End every response with: '⚠️ Educational only. Consult a doctor for medical concerns.'
";

            var requestBody = new
            {
                model = model,
                stream = true,   // ✅ THIS LINE
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userMessage }
                }
            };

            var response = await client.PostAsJsonAsync(
                "https://api.groq.com/openai/v1/chat/completions",
                requestBody);

            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return $"Groq Error: {content}";

            var result = JsonSerializer.Deserialize<OpenAIResponseDto>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return result?.Choices?.FirstOrDefault()?.Message?.Content
                   ?? "No response from AI.";
        }

        public async Task<string> SummarizeAsync(string content)
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
            new {
                role = "system",
                content = "Summarize the following Ayurvedic conversation briefly in 4-5 lines preserving important health context."
            },
            new {
                role = "user",
                content = content
            }
        }
            };

            var response = await client.PostAsJsonAsync(
                "https://api.groq.com/openai/v1/chat/completions",
                requestBody);

            var resultJson = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<OpenAIResponseDto>(
                resultJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return result?.Choices?.FirstOrDefault()?.Message?.Content ?? "";
        }

    }
}
