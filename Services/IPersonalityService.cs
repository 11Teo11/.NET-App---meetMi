namespace ProiectDotNet.Services
{
    // clasa care tine rezultatul analizei ai
    public class PersonalityResult
    {
        public string PersonalityType { get; set; } = "Unknown";
        public bool Success { get; set; } = false;
        public string? ErrorMessage { get; set; }
    }

    // interfata pentru serviciul de personalitate
    public interface IPersonalityService
    {
        // metoda care analizeaza raspunsurile la quiz
        Task<PersonalityResult> AnalyzeQuizAsync(List<string> answers);

        // metoda care calculeaza compatibilitatea intre doua tipuri
        int CalculateCompatibility(string type1, string type2);
    }
}