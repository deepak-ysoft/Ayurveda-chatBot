using Ayurveda_chatBot.DTO;
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

        public async Task<string> GetResponseAsync(string userMessage)
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
                content = @"You are an Ayurvedic educational wellness assistant.
Do NOT provide dosage.
Do NOT claim cure.
If severe symptoms appear advise immediate medical attention.
Always include disclaimer."
            },
            new {
                role = "user",
                content = userMessage
            }
        }
            };

            var response = await client.PostAsJsonAsync(
                "https://api.groq.com/openai/v1/chat/completions",
                requestBody);

            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return $"Groq Error: {content}";
            }

            var result = JsonSerializer.Deserialize<OpenAIResponseDto>(
                content,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return result?.Choices?.FirstOrDefault()?.Message?.Content
                   ?? "No response from AI.";
        }
    }
}
