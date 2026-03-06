using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProiectDotNet.Services
{
    // obiect care stocheaza rezultatul procesului de filtrare
    public class ContentFilterResult
    {
        public bool IsAppropriate { get; set; } = true;
        public bool Success { get; set; } = false;
        public string? ErrorMessage { get; set; }
    }

    // definitia interfetei pentru utilizare in restul aplicatiei
    public interface IContentFilterService
    {
        Task<ContentFilterResult> IsContentSafeAsync(string text);
    }

    // implementarea serviciului folosind infrastructura google ai din 2026
    public class GoogleContentFilterService : IContentFilterService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<GoogleContentFilterService> _logger;

        // setarile pentru endpoint si modelul confirmat in lista de acces
        private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/";
        private const string ModelName = "gemini-2.5-flash";

        public GoogleContentFilterService(IConfiguration configuration, ILogger<GoogleContentFilterService> logger)
        {
            _httpClient = new HttpClient();
            // cheia trebuie sa fie prezenta in appsettings.json sub GoogleAI:ApiKey
            _apiKey = configuration["GoogleAI:ApiKey"] ?? throw new ArgumentNullException("api key is missing from configuration");
            _logger = logger;

            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<ContentFilterResult> IsContentSafeAsync(string text)
        {
            try
            {
                // prompt optimizat pentru a asigura un raspuns json complet si valid
                // am adaugat instructiuni explicite pentru suport bilingv (romana si engleza)
                var prompt = $@"Task: Content Moderation.
                               Analyze the following text for hate speech, violence, or insults in Romanian or English.
                               Response Requirement: Return ONLY a valid JSON object. Do not include markdown or text.
                               Exact Format: {{""isAppropriate"": true}} or {{""isAppropriate"": false}}
                               Text to analyze: ""{text}""";

                var requestBody = new GoogleAiRequest
                {
                    Contents = new List<GoogleAiContent>
                    {
                        new GoogleAiContent { Parts = new List<GoogleAiPart> { new GoogleAiPart { Text = prompt } } }
                    },
                    GenerationConfig = new GoogleAiGenerationConfig
                    {
                        Temperature = 0.0, // temperatura zero asigura raspunsuri deterministe si scurte
                        MaxOutputTokens = 150 // am marit limita pentru a evita intreruperea json-ului
                    }
                };

                var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var jsonContent = JsonSerializer.Serialize(requestBody, jsonOptions);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var requestUrl = $"{BaseUrl}{ModelName}:generateContent?key={_apiKey}";

                var response = await _httpClient.PostAsync(requestUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("google api technical error: {Status} - {Body}", response.StatusCode, responseContent);
                    return new ContentFilterResult { Success = false, ErrorMessage = "ai service unreachable" };
                }

                var googleResponse = JsonSerializer.Deserialize<GoogleAiResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // extragem continutul text generat de model
                var assistantMessage = googleResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

                if (string.IsNullOrWhiteSpace(assistantMessage))
                {
                    return new ContentFilterResult { Success = false, ErrorMessage = "ai returned an empty message" };
                }

                // izolam obiectul json din raspuns pentru a evita erorile de formatare
                var cleanedResponse = ExtractJson(assistantMessage);

                if (string.IsNullOrEmpty(cleanedResponse))
                {
                    _logger.LogWarning("failed to extract json from raw response: {Raw}", assistantMessage);
                    return new ContentFilterResult { Success = false, ErrorMessage = "invalid response format" };
                }

                var filterData = JsonSerializer.Deserialize<FilterResponse>(cleanedResponse);

                return new ContentFilterResult
                {
                    IsAppropriate = filterData?.IsAppropriate ?? true,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "internal error during ai content filtering");
                return new ContentFilterResult { Success = false, ErrorMessage = "internal processing error" };
            }
        }

        // cauta prima si ultima acolada pentru a extrage doar obiectul json valid
        private string ExtractJson(string response)
        {
            var trimmed = response.Trim();
            int start = trimmed.IndexOf('{');
            int end = trimmed.LastIndexOf('}');

            if (start != -1 && end != -1 && end > start)
            {
                return trimmed.Substring(start, (end - start) + 1);
            }
            return string.Empty;
        }
    }

    // modelele de date necesare pentru comunicarea cu protocolul google ai
    public class GoogleAiRequest
    {
        [JsonPropertyName("contents")]
        public List<GoogleAiContent> Contents { get; set; } = new();
        [JsonPropertyName("generationConfig")]
        public GoogleAiGenerationConfig? GenerationConfig { get; set; }
    }

    public class GoogleAiContent { [JsonPropertyName("parts")] public List<GoogleAiPart> Parts { get; set; } = new(); }
    public class GoogleAiPart { [JsonPropertyName("text")] public string Text { get; set; } = ""; }
    public class GoogleAiGenerationConfig { [JsonPropertyName("temperature")] public double Temperature { get; set; } [JsonPropertyName("maxOutputTokens")] public int MaxOutputTokens { get; set; } }
    public class GoogleAiResponse { [JsonPropertyName("candidates")] public List<GoogleAiCandidate>? Candidates { get; set; } }
    public class GoogleAiCandidate { [JsonPropertyName("content")] public GoogleAiContent? Content { get; set; } }
    public class FilterResponse { [JsonPropertyName("isAppropriate")] public bool IsAppropriate { get; set; } }
}