using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProiectDotNet.Data;
using ProiectDotNet.Models;

namespace ProiectDotNet.Controllers
{
    [Authorize]
    public class GroupMembersController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;

        // constructor pentru injectarea bazei de date si a managerului de utilizatori
        public GroupMembersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            db = context;
            _userManager = userManager;
        }

        // logica pentru intrarea intr-un grup sau trimiterea unei cereri de acces
        [HttpPost]
        public IActionResult Join(int groupId)
        {
            var userId = _userManager.GetUserId(User);
            var group = db.Groups.Find(groupId);
            if (group == null) return NotFound();

            // aici verific sa nu am duplicate la cereri (daca ai cerut deja sa fii membru al grupului, nu poti cere iar)
            if (db.GroupMembers.Any(gm => gm.GroupId == groupId && gm.UserId == userId)) return BadRequest();

            // isaccepted devine true automat daca grupul e public, altfel ramane false (cerere in asteptare)
            db.GroupMembers.Add(new GroupMember { GroupId = groupId, UserId = userId, IsAccepted = group.IsPublic });
            db.SaveChanges();
            return RedirectToAction("Show", "Groups", new { id = groupId });
        }

        // afisarea listei de cereri in asteptare pentru moderator
        public IActionResult Requests(int groupId)
        {
            // incarcam grupul si lista de membri care vor sa adere
            var group = db.Groups.Include(g => g.GroupMembers).ThenInclude(gm => gm.User).FirstOrDefault(g => g.Id == groupId);
            // doar moderatorul sau adminul pot gestiona aceste cereri
            if (group == null || (group.ModeratorId != _userManager.GetUserId(User) && !User.IsInRole("Admin"))) return Forbid();

            ViewBag.GroupName = group.Name;
            ViewBag.GroupId = group.Id;

            // trimitem catre view doar membrii care nu au fost inca acceptati
            return View(group.GroupMembers.Where(gm => !gm.IsAccepted).ToList());
        }

        // acceptare sau respingere cerere
        [HttpPost]
        public IActionResult ProcessRequest(int groupId, string userId, bool accept)
        {
            var group = db.Groups.Find(groupId);
            if (group == null || (group.ModeratorId != _userManager.GetUserId(User) && !User.IsInRole("Admin"))) return Forbid();

            var member = db.GroupMembers.FirstOrDefault(gm => gm.GroupId == groupId && gm.UserId == userId);
            if (member != null)
            {
                if (accept) member.IsAccepted = true; // aprobam cererea
                else db.GroupMembers.Remove(member); // respingem cererea prin eliminarea ei din lista
                db.SaveChanges();
            }
            return RedirectToAction("Requests", new { groupId });
        }

        // eliminarea unui membru sau pentru parasirea voluntara a grupului
        [HttpPost]
        public IActionResult Remove(int groupId, string userId)
        {
            var currentUserId = _userManager.GetUserId(User);
            var group = db.Groups.Find(groupId);

            if (group == null) return NotFound();

            // doar moderatorul poate elimina pe cineva, sau user-ul poate alege sa plece singur
            if (group.ModeratorId != currentUserId && currentUserId != userId) return Forbid();
            if (group.ModeratorId == userId) return BadRequest(); // moderatorul nu poate iesi fara sa stearga grupul

            var member = db.GroupMembers.FirstOrDefault(gm => gm.GroupId == groupId && gm.UserId == userId);
            if (member != null)
            {
                db.GroupMembers.Remove(member);
                db.SaveChanges();
            }

            // redirectionam la index daca user-ul a iesit, sau ramanem pe pagina grupului daca moderatorul a eliminat pe cineva
            return RedirectToAction(currentUserId == userId ? "Index" : "Show", "Groups", new { id = groupId });
        }
    }
}