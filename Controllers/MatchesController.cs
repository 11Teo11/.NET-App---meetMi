using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProiectDotNet.Data;
using ProiectDotNet.Models;
using ProiectDotNet.Services;

namespace ProiectDotNet.Controllers
{
    [Authorize] // doar utilizatorii logati pot vedea recomandari
    public class MatchesController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPersonalityService _personalityService;

        public MatchesController(ApplicationDbContext context,
                                 UserManager<ApplicationUser> userManager,
                                 IPersonalityService personalityService)
        {
            db = context;
            _userManager = userManager;
            _personalityService = personalityService;
        }

        public async Task<IActionResult> Index()
        {
            var currentUserId = _userManager.GetUserId(User);
            var currentUser = await db.Users.FirstOrDefaultAsync(u => u.Id == currentUserId);

            if (currentUser == null || string.IsNullOrEmpty(currentUser.PersonalityType))
            {
                // daca userul nu a facut testul, il trimitem sa il faca
                return RedirectToAction("Index", "Quiz");
            }

            // preluam toti ceilalti utilizatori
            var otherUsers = await db.Users
                .Where(u => u.Id != currentUserId)
                .ToListAsync();

            var recommendations = new List<UserMatchViewModel>();

            foreach (var user in otherUsers)
            {
                // calculam scorul de compatibilitate folosind serviciul existent
                var score = _personalityService.CalculateCompatibility(currentUser.PersonalityType, user.PersonalityType);

                // verificam daca il urmarim deja (pentru a afisa butonul corect)
                var isFollowing = await db.Requests.AnyAsync(r => r.SenderId == currentUserId
                                                               && r.ReceiverId == user.Id
                                                               && r.Status == "accepted");

                recommendations.Add(new UserMatchViewModel
                {
                    User = user,
                    CompatibilityScore = score,
                    IsFollowing = isFollowing
                });
            }

            // sortam dupa cel mai mare scor de compatibilitate
            var sortedMatches = recommendations.OrderByDescending(m => m.CompatibilityScore).ToList();

            ViewBag.CurrentUserPersonality = currentUser.PersonalityType;
            return View(sortedMatches);
        }
    }

    // clasa auxiliara pentru a stoca datele despre match in view
    public class UserMatchViewModel
    {
        public ApplicationUser User { get; set; }
        public int CompatibilityScore { get; set; }
        public bool IsFollowing { get; set; }
    }
}