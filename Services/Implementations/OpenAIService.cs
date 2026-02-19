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
     Dosha dosha)
        {
            var apiKey = _configuration["Groq:ApiKey"];
            var model = _configuration["Groq:Model"];

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);

            var systemPrompt = $@"
You are an Ayurvedic educational wellness assistant.

User Profile:
Age: {user.Age}
Gender: {user.Gender}
Diet: {user.Diet}
Weight: {user.Weight}

Primary Dosha: {dosha.Name}

Dosha Details:
Qualities: {dosha.Qualities}
Common Imbalance Symptoms: {dosha.ImbalanceSymptoms}
Diet Advice: {dosha.DietAdvice}
Lifestyle Advice: {dosha.LifestyleAdvice}

Rules:
- Respond strictly using Ayurvedic principles.
- Personalize response based on user's dosha.
- Do NOT provide dosage.
- Do NOT claim cure.
- If severe symptoms appear advise immediate medical attention.
- Always include disclaimer at the end.
";

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

            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return $"Groq Error: {content}";

            var result = JsonSerializer.Deserialize<OpenAIResponseDto>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return result?.Choices?.FirstOrDefault()?.Message?.Content
                   ?? "No response from AI.";
        }

    }
}
