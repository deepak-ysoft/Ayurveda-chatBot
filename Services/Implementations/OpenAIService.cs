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
You are an Ayurvedic educational wellness assistant.

User Profile:
Age: {user.Age}
Gender: {user.Gender}
Diet: {user.Diet}
Weight: {user.Weight}

Primary Dosha: {dosha.Name}

Previous Conversation Summary:
{summarizedContext}

Instructions:
- If current question relates to previous conversation, continue accordingly.
- If unrelated, ignore previous summary.
- Respond strictly using Ayurvedic principles.
- No dosage.
- No cure claims.
- Advise medical attention if severe.
- Add disclaimer.
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
