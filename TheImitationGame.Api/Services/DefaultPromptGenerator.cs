using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace TheImitationGame.Api.Services
{
    public class DefaultPromptGenerator
    {
        private readonly string? _apiKey;

        const string systemPrompt =
            "You are a prompt generator for a casual drawing game like Gartic Phone. " +
            "Reply with a single phrase suitable as a drawing prompt, with no surrounding quotes or extra commentary. " +
            "There should be no full stop at the end of the prompt. " +
            "The prompt should be a situation, character, or interaction that could be easily drawn. " +
            "The user may choose to use a different prompt to give to the other player than the one you generate, so yours should be simple and to-the-point";
        const string userPrompt = "Generate a short, simple, one-phrase drawing prompt.";

        const string fallbackPrompt = "A cat exploding";


        public DefaultPromptGenerator(IConfiguration configuration)
        {
            _apiKey = configuration["OpenAI:ApiKey"];
        }

        public async Task<string> GenerateDefaultPromptAsync()
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
                return fallbackPrompt;

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var requestBody = new
            {
                model = "gpt-3.5-turbo",
                messages = new[] {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                max_tokens = 30,
                temperature = 0.9
            };

            var json = JsonSerializer.Serialize(requestBody);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                using var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"OpenAI error response: {(int)response.StatusCode} {response.StatusCode}\n{body}");
                    return fallbackPrompt;
                }

                using var docSuccess = JsonDocument.Parse(body);
                if (docSuccess.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var first = choices[0];
                    if (first.TryGetProperty("message", out var message) && message.TryGetProperty("content", out var contentEl))
                    {
                        var result = contentEl.GetString()?.Trim();
                        if (!string.IsNullOrWhiteSpace(result))
                            return result;
                    }
                }

                Console.WriteLine($"OpenAI returned no content. Body:\n{body}");
                return fallbackPrompt;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OpenAI request exception: {ex}");
                return fallbackPrompt;
            }
        }
    }
}