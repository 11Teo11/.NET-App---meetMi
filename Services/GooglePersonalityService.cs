using System.Text;
using System.Text.Json;
using ProiectDotNet.Models;

namespace ProiectDotNet.Services
{
    public class GooglePersonalityService : IPersonalityService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<GooglePersonalityService> _logger;
        private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/";
        private const string ModelName = "gemini-2.5-flash";

        public GooglePersonalityService(IConfiguration configuration, ILogger<GooglePersonalityService> logger)
        {
            _httpClient = new HttpClient();
            _apiKey = configuration["GoogleAI:ApiKey"] ?? throw new ArgumentNullException("googleai key missing");
            _logger = logger;
        }

        public async Task<PersonalityResult> AnalyzeQuizAsync(List<string> answers)
        {
            try
            {
                // concatenam toate cele 15 raspunsuri pentru a fi trimise catre ai
                var combinedAnswers = string.Join(" | ", answers);

                // prompt optimizat pentru analiza celor 15 puncte de date
                var prompt = $@"Analyze these 15 social personality answers: '{combinedAnswers}'. 
                Determine which category fits best: 'The Initiator', 'The Explorer', 'The Diplomat', or 'The Analyst'.
                Respond ONLY with a valid JSON object: {{""personalityType"": ""category_name""}}";

                var requestUrl = $"{BaseUrl}{ModelName}:generateContent?key={_apiKey}";

                var requestBody = new
                {
                    contents = new[] { new { parts = new[] { new { text = prompt } } } }
                };

                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(requestUrl, content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode) return new PersonalityResult { Success = false };

                using var doc = JsonDocument.Parse(responseString);
                var aiText = doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();

                // curatam string-ul de eventuale marcaje markdown de tip block code
                var cleanedJson = aiText?.Replace("```json", "").Replace("```", "").Trim();
                var result = JsonSerializer.Deserialize<PersonalityResult>(cleanedJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result != null) result.Success = true;
                return result ?? new PersonalityResult { Success = false };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "eroare la analiza celor 15 raspunsuri de personalitate");
                return new PersonalityResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        public int CalculateCompatibility(string type1, string type2)
        {
            if (string.IsNullOrEmpty(type1) || string.IsNullOrEmpty(type2)) return 0;
            if (type1 == type2) return 100;

            // matrice de compatibilitate bazata pe arhetipurile meetmi
            return (type1, type2) switch
            {
                ("The Initiator", "The Explorer") => 95,
                ("The Analyst", "The Diplomat") => 90,
                ("The Initiator", "The Diplomat") => 85,
                ("The Explorer", "The Analyst") => 75,
                _ => 60
            };
        }
    }
}