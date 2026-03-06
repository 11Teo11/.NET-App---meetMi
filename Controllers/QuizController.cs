using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProiectDotNet.Models;
using ProiectDotNet.Services;

namespace ProiectDotNet.Controllers
{
    [Authorize]
    public class QuizController : Controller
    {
        private readonly IPersonalityService _personalityService;
        private readonly UserManager<ApplicationUser> _userManager;

        public QuizController(IPersonalityService personalityService, UserManager<ApplicationUser> userManager)
        {
            _personalityService = personalityService;
            _userManager = userManager;
        }

        public IActionResult Index() => View();

        // metoda noua pentru a afisa pagina de rezultate direct din meniu
        public async Task<IActionResult> Results()
        {
            var user = await _userManager.GetUserAsync(User);
            var result = new PersonalityResult
            {
                PersonalityType = user?.PersonalityType ?? "Unknown",
                Success = true
            };
            return View(result);
        }

        [HttpPost]
        public async Task<IActionResult> Submit(List<string> answers)
        {
            // validam ca toate cele 15 intrebari au primit un raspuns
            if (answers == null || answers.Count < 15)
            {
                return RedirectToAction("Index");
            }

            // trimitem datele catre ai pentru analiza
            var result = await _personalityService.AnalyzeQuizAsync(answers);

            if (result.Success)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    // salvam categoria determinata de ai in profilul utilizatorului
                    user.PersonalityType = result.PersonalityType;
                    user.PersonalityTestedAt = DateTime.Now;
                    await _userManager.UpdateAsync(user);
                }
                return View("Results", result);
            }

            return View("Error");
        }
    }
}