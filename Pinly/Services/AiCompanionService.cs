using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Diagnostics;
using Microsoft.Extensions.Configuration; 

namespace Pinly.Services
{
    public class AiCompanionService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey; // Stocam cheia aici

        private const string AiUrl = "https://router.huggingface.co/hf-inference/models/unitary/toxic-bert";

        public AiCompanionService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;

            _apiKey = configuration["HuggingFace:ApiKey"];

            if (string.IsNullOrEmpty(_apiKey))
            {
                Debug.WriteLine("⚠️ ATENTIE: Cheia HuggingFace nu a fost gasita in configurari!");
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        public async Task<bool> IsSafe(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return true;
            if (string.IsNullOrEmpty(_apiKey)) return true; // Daca nu avem cheie, lasam sa treaca (fail open)

            try
            {
                var payload = new { inputs = text };
                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(AiUrl, content);
                var responseString = await response.Content.ReadAsStringAsync();

                // Debug.WriteLine($"[AI STATUS] Code: {response.StatusCode}");

                if (!response.IsSuccessStatusCode) return true;

                using var doc = JsonDocument.Parse(responseString);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                {
                    var firstElement = root[0];
                    if (firstElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in firstElement.EnumerateArray())
                        {
                            var label = item.GetProperty("label").GetString();
                            var score = item.GetProperty("score").GetDouble();

                            var badLabels = new[] { "toxic", "severe_toxic", "obscene", "threat", "insult", "identity_hate" };

                            if (badLabels.Contains(label) && score > 0.60)
                            {
                                return false; // BLOCAT
                            }
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AI ERROR]: {ex.Message}");
                return true;
            }
        }
    }
}