using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProiectDotNet.Data;
using ProiectDotNet.Models;
using ProiectDotNet.Services;

namespace ProiectDotNet.Controllers
{
    [Authorize]
    public class MessagesController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IContentFilterService _contentFilter;


        // constructor pentru injectarea dependintelor
        public MessagesController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IContentFilterService contentFilter)
        {
            db = context;
            _userManager = userManager;
            _contentFilter = contentFilter;
        }

        // crearea mesajului cu filtrare ai obligatorie
        [HttpPost]
        public async Task<IActionResult> Create(int groupId, string content, IFormFile? media, [FromServices] IWebHostEnvironment env)
        {
            var userId = _userManager.GetUserId(User);
            var isMember = db.GroupMembers.Any(gm => gm.GroupId == groupId && gm.UserId == userId && gm.IsAccepted);

            if (!isMember && !User.IsInRole("Admin")) return Forbid();

            // verificam continutul prin companionul ai
            if (!string.IsNullOrEmpty(content))
            {
                var filterResult = await _contentFilter.IsContentSafeAsync(content);

                // verificam daca eroarea este de la ai (continut nepotrivit)
                if (filterResult.Success && !filterResult.IsAppropriate)
                {
                    TempData["message"] = "Your content contains inappropriate terms. Please rephrase.";
                    TempData["messageType"] = "alert-danger";
                    return RedirectToAction("Show", "Groups", new { id = groupId });
                }
                // daca filterResult.Success este false, inseamna ca api-ul openAi are o problema
                // in acest caz, pentru testare afisam eroarea reala
                if (!filterResult.Success)
                {
                    TempData["message"] = "AI technical error " + filterResult.ErrorMessage;
                    TempData["messageType"] = "alert-warning";
                    return RedirectToAction("Show", "Groups", new { id = groupId });
                }
            }

            var message = new ProiectDotNet.Models.Message { GroupId = groupId, UserId = userId, Content = content, Date = DateTime.Now };

            if (media != null && media.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(media.FileName);
                var path = Path.Combine(env.WebRootPath, "images", fileName);
                using (var stream = new FileStream(path, FileMode.Create)) { await media.CopyToAsync(stream); }
                message.Media = "/images/" + fileName;
            }

            if (!string.IsNullOrEmpty(message.Content) || !string.IsNullOrEmpty(message.Media))
            {
                db.Messages.Add(message);
                db.SaveChanges();
            }
            return RedirectToAction("Show", "Groups", new { id = groupId });
        }

        // editare mesaj cu aceeasi logica de filtrare obligatorie
        [HttpPost]
        public async Task<IActionResult> Edit(int id, string content)
        {
            var message = db.Messages.Find(id);
            if (message == null || message.UserId != _userManager.GetUserId(User)) return Forbid();

            if (!string.IsNullOrEmpty(content))
            {
                var filterResult = await _contentFilter.IsContentSafeAsync(content);

                // daca serviciul ai nu este disponibil sau detecteaza limbaj neadecvat, oprim salvarea
                if (!filterResult.Success || !filterResult.IsAppropriate)
                {
                    TempData["message"] = "Your content contains inappropriate terms. Please rephrase.";
                    TempData["messageType"] = "alert-danger";
                    return RedirectToAction("Show", "Groups", new { id = message.GroupId });
                }
            }

            message.Content = content;
            db.SaveChanges();
            return RedirectToAction("Show", "Groups", new { id = message.GroupId });
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var message = db.Messages.Include(m => m.Group).FirstOrDefault(m => m.Id == id);
            if (message == null) return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            bool canDelete = message.UserId == currentUserId || message.Group?.ModeratorId == currentUserId || User.IsInRole("Admin");

            if (canDelete)
            {
                db.Messages.Remove(message);
                db.SaveChanges();
                return RedirectToAction("Show", "Groups", new { id = message.GroupId });
            }
            return Forbid();
        }
    }
}