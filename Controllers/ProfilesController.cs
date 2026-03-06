using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProiectDotNet.Data;
using ProiectDotNet.Models;
using ProiectDotNet.Services;

namespace ProiectDotNet.Controllers
{
    public class ProfilesController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPersonalityService _personalityService;

        private readonly SignInManager<ApplicationUser> _signInManager;


        public ProfilesController(ApplicationDbContext context,
                                  UserManager<ApplicationUser> userManager,
                                  IPersonalityService personalityService,
                                  SignInManager<ApplicationUser> signInManager)
        {
            db = context;
            _userManager = userManager;
            _personalityService = personalityService;
            _signInManager = signInManager;
        }

        // actiunea ce afiseaza profilul unui user
        public IActionResult Show(string id)
        {
            // luam id-ul user-ului curent
            var currentUserId = _userManager.GetUserId(User);

            // cautam user-ul caruia vrem sa ii vedem profilul dupa id, inclusiv postarile lui
            ApplicationUser? user = db.Users
                                      .Include(u => u.Posts)
                                          .ThenInclude(p => p.Reactions)
                                      .Include(u => u.Posts)
                                          .ThenInclude(p => p.Comments)
                                            .ThenInclude(c => c.User)
                                      .FirstOrDefault(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            // calculam numarul de urmaritori folosind tabela requests
            ViewBag.FollowersCount = db.Requests.Count(r => r.ReceiverId == id
                                                         && r.Status == "accepted");
            ViewBag.FollowingCount = db.Requests.Count(r => r.SenderId == id
                                                         && r.Status == "accepted");

            // verificam statusul dintre user-ul curent si profilul vizualizat
            bool isFollowing = false;
            bool isPending = false;
            bool isOwnProfile = false;

            // setam tipul de personalitate pentru view
            ViewBag.TargetPersonalityType = user.PersonalityType;

            if (currentUserId != null)
            {
                if (currentUserId == id)
                    isOwnProfile = true;
                else
                {
                    var request = db.Requests.FirstOrDefault(r => r.SenderId == currentUserId
                                                            && r.ReceiverId == id);
                    if (request != null)
                    {
                        if (request.Status == "accepted")
                            isFollowing = true;
                        else if (request.Status == "pending")
                            isPending = true;
                    }

                    var currentUser = db.Users.Find(currentUserId);
                    if (currentUser != null)
                    {
                        ViewBag.CompatibilityScore = _personalityService.CalculateCompatibility(currentUser.PersonalityType, user.PersonalityType);
                    }
                }
            }

            // lista de postari ale profilului
            List<Post> profilePosts;

            if (user.Visibility == "private" && !isOwnProfile && !isFollowing)
            {
                profilePosts = [];
                ViewBag.IsProfileHidden = true;
            }
            else
            {
                profilePosts = user.Posts.OrderByDescending(p => p.Date).ToList();
                ViewBag.IsProfileHidden = false;
            }

            ViewBag.UserPosts = profilePosts;
            ViewBag.IsOwnProfile = isOwnProfile;
            ViewBag.IsFollowing = isFollowing;
            ViewBag.IsPending = isPending;

            if (currentUserId != null)
            {
                var currentUser = db.Users.Find(currentUserId);
                ViewBag.UserProfilePic = currentUser?.ProfilePicture;
                ViewBag.UserCurent = currentUserId;
            }

            return View(user);
        }

        // actiunea pentru butonul de follow sau unfollow
        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public IActionResult ToggleFollow(string id)
        {
            var currentUserId = _userManager.GetUserId(User);

            if (currentUserId == id)
                return RedirectToAction("Show", new { id = id });

            var targetUser = db.Users.Find(id);
            if (targetUser == null)
                return NotFound();

            var request = db.Requests.FirstOrDefault(r => r.SenderId == currentUserId
                                                       && r.ReceiverId == id);
            if (request != null)
            {
                // daca exista deja relatia sterge cererea pentru unfollow
                db.Requests.Remove(request);
            }
            else
            {
                // creeaza o cerere noua
                var newRequest = new Request
                {
                    SenderId = currentUserId,
                    ReceiverId = id
                };

                if (targetUser.Visibility == "public")
                    newRequest.Status = "accepted";
                else
                    newRequest.Status = "pending";

                db.Requests.Add(newRequest);
            }

            db.SaveChanges();
            return RedirectToAction("Show", new { id = id });
        }

        // formularul de editare a profilului
        [Authorize]
        public IActionResult Edit()
        {
            var currentUserId = _userManager.GetUserId(User);
            var user = db.Users.Find(currentUserId);
            ViewBag.UserProfilePic = user?.ProfilePicture;
            return View(user);
        }

        // salveaza modificarile profilului
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Edit(ApplicationUser requestUser, IFormFile? ProfileImage)
        {
            var currentUserId = _userManager.GetUserId(User);
            var user = db.Users.Find(currentUserId);

            if (user == null)
                return NotFound();

            user.FirstName = requestUser.FirstName;
            user.LastName = requestUser.LastName;
            user.Description = requestUser.Description;
            user.Visibility = requestUser.Visibility;

            if (ProfileImage != null && ProfileImage.Length > 0)
            {
                var storagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", ProfileImage.FileName);
                using (var fileStream = new FileStream(storagePath, FileMode.Create))
                {
                    await ProfileImage.CopyToAsync(fileStream);
                }
                user.ProfilePicture = "/images/" + ProfileImage.FileName;
            }

            db.SaveChanges();
            return RedirectToAction("Show", new { id = currentUserId });
        }

       // adaugam actiunea de stergere in ProfilesController.cs
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Delete(string id)
        {
            var currentUserId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");

            // verificam daca cel care sterge este admin sau este propriul profil
            if (currentUserId != id && !isAdmin)
            {
                return Forbid();
            }

            var user = await db.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // daca utilizatorul se sterge pe sine, il delogam inainte ca profilul sa dispara
            if (currentUserId == id)
            {
                await _signInManager.SignOutAsync();
            }

            // stergem utilizatorul
            db.Users.Remove(user);
            await db.SaveChangesAsync();

            // daca utilizatorul s-a sters pe sine, il redirectionam catre login/home si sesiunea va expira
            // daca adminul a sters pe cineva, il trimitem la pagina principala
            return RedirectToAction("Index", "Home");
        }



        [HttpPost]
        [Authorize]
        [IgnoreAntiforgeryToken]
        // actiunea care proceseaza acceptarea sau refuzarea unei cereri
        public async Task<IActionResult> HandleRequest([FromForm] int requestId, [FromForm] string status)
        {
            var currentUserId = _userManager.GetUserId(User);

            // cautam cererea verificand si daca user-ul curent este destinatarul
            // fix pentru eroarea de cautare cu cheie primara compusa
            var request = await db.Requests.FirstOrDefaultAsync(r => r.Id == requestId && r.ReceiverId == currentUserId);

            if (request == null)
            {
                return Json(new { success = false, message = "Invalid request or unauthorized." });
            }

            request.Status = status;
            await db.SaveChangesAsync();

            return Json(new { success = true, newStatus = status });
        }
    }
}