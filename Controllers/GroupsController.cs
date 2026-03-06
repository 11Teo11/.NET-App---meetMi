using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProiectDotNet.Data;
using ProiectDotNet.Models;

namespace ProiectDotNet.Controllers
{
    [Authorize]
    public class GroupsController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;

        // constructorul in care se injecteaza contextul bazei de date si managerul de utilizatori
        public GroupsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            db = context;
            _userManager = userManager;
        }

        // afisarea listei de comunitati pentru toti vizitatorii
        [AllowAnonymous]
        public IActionResult Index()
        {
            // se iau toate grupurile din baza de date impreuna cu moderatorii lor
            var groups = db.Groups.Include(g => g.Moderator).ToList();
            ViewBag.Groups = groups;

            // preluarea mesajelor din tempdata pentru afisarea alertelor dupa actiuni
            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }
            return View();
        }

        // afisarea paginii unui grup, incarcand mesajele, membrii si verificand permisiunile
        public IActionResult Show(int id)
        {
            // id-ul utilizatorului logat pentru a verifica relatia cu grupul
            var currentUserId = _userManager.GetUserId(User);
            // cautam grupul cu toate datele conexe (moderator, mesaje, membri)
            var group = db.Groups
                .Include(g => g.Moderator)
                .Include(g => g.Messages).ThenInclude(m => m.User)
                .Include(g => g.GroupMembers).ThenInclude(gm => gm.User)
                .FirstOrDefault(g => g.Id == id);

            if (group == null) return NotFound(); // daca nu exista grupul cautat, returnam NotFound

            // calculam statusul utilizatorului fata de grupul curent
            var groupMember = group.GroupMembers.FirstOrDefault(gm => gm.UserId == currentUserId);
            bool isMember = groupMember != null && groupMember.IsAccepted;
            bool isModerator = group.ModeratorId == currentUserId;
            bool isAdmin = User.IsInRole("Admin");

            // salvam datele in viewbag pentru a fi folosite in interfata (afisare butoane/continut)
            ViewBag.IsMember = isMember;
            ViewBag.IsModerator = isModerator;
            ViewBag.IsAdmin = isAdmin;
            ViewBag.IsPending = groupMember != null && !groupMember.IsAccepted;
            ViewBag.UserCurent = currentUserId;
            ViewBag.PendingRequestsCount = group.GroupMembers.Count(gm => !gm.IsAccepted);

            // restrictionam accesul daca grupul este privat si user-ul nu are permisiuni
            if (!group.IsPublic && !isMember && !isModerator && !isAdmin)
            {
                ViewBag.RestrictedAccess = true;
            }

            return View(group);
        }

        // afisare formular creare grup nou
        public IActionResult New() => View(new Group());

        // procesarea crearii unui grup si salvarea pozei de coperta
        [HttpPost]
        public async Task<IActionResult> New(Group group, IFormFile? CoverPhotoFile, [FromServices] IWebHostEnvironment env)
        {
            // setam id-ul moderatorului ca fiind user-ul logat
            group.ModeratorId = _userManager.GetUserId(User);

            // daca s-a incarcat o poza de coperta, o salvam
            if (CoverPhotoFile != null && CoverPhotoFile.Length > 0)
            {
                // generam un nume unic pentru fisier
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(CoverPhotoFile.FileName);
                var path = Path.Combine(env.WebRootPath, "images", fileName);
                using (var stream = new FileStream(path, FileMode.Create)) { await CoverPhotoFile.CopyToAsync(stream); }

                // salvam calea in baza de date
                group.CoverPhoto = "/images/" + fileName;
            }

            if (ModelState.IsValid)
            {
                // adaugam grupul in baza de date
                db.Groups.Add(group);
                db.SaveChanges();
                // moderatorul devine automat membru acceptat al grupului creat
                db.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = group.ModeratorId, IsAccepted = true });
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(group);
        }

        // afisare pagina de editare a informatiilor grupului
        public IActionResult Edit(int id)
        {
            var group = db.Groups.Find(id);
            if (group == null) return NotFound();

            // verificam daca cel care editeaza este moderatorul grupului sau un admin
            if (group.ModeratorId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            {
                ViewBag.UserCurent = _userManager.GetUserId(User);
                return View(group);
            }
            return RedirectToAction("Index");
        }

        // procesarea actualizarilor datelor grupului
        [HttpPost]
        public async Task<IActionResult> Edit(int id, Group requestGroup, IFormFile? CoverPhotoFile, [FromServices] IWebHostEnvironment env)
        {
            var group = db.Groups.Find(id);
            // doar moderatorul are dreptul de a modifica datele
            if (group == null || group.ModeratorId != _userManager.GetUserId(User)) return Forbid();

            // actualizarea pozei de coperta daca s-a incarcat un fisier nou
            if (CoverPhotoFile != null && CoverPhotoFile.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(CoverPhotoFile.FileName);
                var path = Path.Combine(env.WebRootPath, "images", fileName);
                using (var stream = new FileStream(path, FileMode.Create)) { await CoverPhotoFile.CopyToAsync(stream); }
                group.CoverPhoto = "/images/" + fileName;
            }

            if (ModelState.IsValid)
            {
                // actualizam campurile modificate
                group.Name = requestGroup.Name;
                group.Description = requestGroup.Description;
                group.IsPublic = requestGroup.IsPublic;
                db.SaveChanges();
                return RedirectToAction("Show", new { id });
            }
            return View(requestGroup);
        }

        // stergerea grupului si a tuturor datelor dependente (mesaje, membri)
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var group = db.Groups.Find(id);
            if (group == null) return NotFound();

            // verificam dreptul de stergere (moderator sau admin)
            if (group.ModeratorId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            {
                // stergem mai intai dependentele pentru a pastra integritatea bazei de date
                db.GroupMembers.RemoveRange(db.GroupMembers.Where(gm => gm.GroupId == id));
                db.Messages.RemoveRange(db.Messages.Where(m => m.GroupId == id));
                // stergem grupul propriu-zis
                db.Groups.Remove(group);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return Forbid();
        }
    }
}