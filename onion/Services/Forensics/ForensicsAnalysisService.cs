using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace onion.Services
{
    public class ForensicsAnalysisService : IForensicsAnalysisService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ForensicsAnalysisService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<string> AnalyzeWebsiteAsync(string targetUrl)
        {
            // Create an HttpClient with a User-Agent header.
            var client = _httpClientFactory.CreateClient();
            if (!client.DefaultRequestHeaders.Contains("User-Agent"))
            {
                client.DefaultRequestHeaders.Add("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
                    "(KHTML, like Gecko) Chrome/90.0.4430.85 Safari/537.36");
            }

            // Fetch the target website's HTML.
            var websiteResponse = await client.GetAsync(targetUrl);
            if (!websiteResponse.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to fetch website. Status: {websiteResponse.StatusCode}");
            }
            var rawHtml = await websiteResponse.Content.ReadAsStringAsync();

            // Optionally truncate the HTML if it's too long.
            int maxLength = 10000;
            if (rawHtml.Length > maxLength)
            {
                rawHtml = rawHtml.Substring(0, maxLength);
            }

            // Build the request payload for the AI model.
            var requestBody = new
            {
                model = "deepseek-r1-distill-qwen-1.5b",
                messages = new object[]
                {
                    new { role = "system", content = "You are going to receive HTML code of a website. What is the website about? Respond in English." },
                    new { role = "user", content = $"Website HTML code:\n\n{rawHtml}" }
                },
                temperature = 0.7,
                max_tokens = 2048,
                stream = false
            };

            var postContent = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            // Call the LM Studio AI model.
            var aiResponse = await client.PostAsync("http://localhost:1234/v1/chat/completions", postContent);
            if (!aiResponse.IsSuccessStatusCode)
            {
                throw new Exception($"AI request failed. Status: {aiResponse.StatusCode}");
            }

            // Read and parse the LM Studio response as JSON.
            var aiResponseRaw = await aiResponse.Content.ReadAsStringAsync();
            using var aiDoc = JsonDocument.Parse(aiResponseRaw);

            // Verify that the response contains at least one choice.
            if (!aiDoc.RootElement.TryGetProperty("choices", out JsonElement choices) ||
                choices.GetArrayLength() == 0)
            {
                return "No response received.";
            }

            // Extract the AI's message content.
            var aiContent = choices[0].GetProperty("message").GetProperty("content").GetString() ?? "";

            // Remove the <think>...</think> block from the content.
            string cleanedContent = Regex.Replace(aiContent, @"<think>[\s\S]*?</think>", "").Trim();

            return cleanedContent;
        }
    }
}
