using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProiectDotNet.Data;
using ProiectDotNet.Models;
using ProiectDotNet.Services; // adaugam referinta pentru a folosi filtrul ai

namespace ProiectDotNet.Controllers
{
    // nu mai are lista de initializari -> am declarat constructorul
    public class PostsController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IWebHostEnvironment _env;
        private readonly IContentFilterService _contentFilter; // proprietate pentru serviciul de filtrare continut

        // contructorul -> in care face dependency injection
        // am adaugat serviciul ai in constructor pentru a-l putea folosi in metode
        public PostsController(
            ApplicationDbContext context,
            IWebHostEnvironment env,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IContentFilterService contentFilter)
        {
            // alocam conexiunea (injectata) cu baza de date unei proprietati locale 
            // pentru a fi refolosita in metodele controller-ului
            db = context;
            _env = env;
            _userManager = userManager;
            _roleManager = roleManager;
            _contentFilter = contentFilter; // initializam serviciul de filtrare
        }


        // lista tuturor postarilor (cu continutul lor) + user ul care a facut postarea
        // [HttpGet] care se executa implicit

        // toti vizitatorii -> inclusiv cei neautentificati pot vedea postarile
        public IActionResult Index()
        {
            // extragem id ul user ului ca sa putem sa punem in ViewBag si poza lui de profil
            var currentUserId = _userManager.GetUserId(User);

            if (currentUserId == null)
            {
                ViewBag.Posts = new List<Post>(); // lista goala
                return View();
            }

            // construim query-ul de baza dar nu il executam inca (nu punem ToList)
            // folosim IQueryable ca sa putem adauga filtre ulterior
            IQueryable<Post> postsQuery = db.Posts
                    .Include(p => p.User)
                    .Include(p => p.Comments.OrderByDescending(c => c.Date))
                        .ThenInclude(c => c.User)
                    .Include(p => p.Reactions);


            // pentru cand ajunge inapoi in Index (din New, Edit sau Delete)
            // si sunt mesaje de afisat 
            if (TempData.ContainsKey("message"))
            {
                // pune mesajul in viewbag
                ViewBag.Message = TempData["message"];
                // pune tipul mesajului in viewbag
                ViewBag.Alert = TempData["messageType"];
            }

            if (currentUserId != null)
            {
                // extragem user ul din baza de date
                var currentUser = db.Users.FirstOrDefault(u => u.Id == currentUserId);

                // salvam calea pozei in ViewBag
                ViewBag.UserProfilePic = currentUser?.ProfilePicture;
                ViewBag.UserCurent = currentUser;

                // extragem id urile userilor pe care ii urmareste userul curent pentru a afisa doar postarile lor
                var followingIds = db.Requests
                    .Where(r => r.SenderId == currentUserId && r.Status == "accepted")
                    .Select(r => r.ReceiverId)
                    .ToList();

                // filtram query-ul -> doar postarile userului curent si a celor pe care ii urmareste
                postsQuery = postsQuery.Where(p => p.UserId == currentUserId || followingIds.Contains(p.UserId));

                // extragem cererile de follow primite
                var incomingRequests = db.Requests
                    .Include(r => r.Sender)
                    .Where(r => r.ReceiverId == currentUserId)
                    .OrderByDescending(r => r.Id)
                    .ToList();

                ViewBag.IncomingRequests = incomingRequests;
            }

            var finalPosts = postsQuery.OrderByDescending(p => p.Date).ToList();

            ViewBag.Posts = finalPosts;

            return View();
        }

        // se afiseaza o singura postare in functie de id-ul sau
        // + toate comentariile asociate postarii 
        // + user ul care a facut postarea respectiva
        // [HttpGet] se executa implicit



        //  toti vizitatorii -> inclusiv cei neautentificati pot vedea o anumita postare

        public IActionResult Show(int id)
        {
            // se preiau toate postarile din baza de date
            Post? post = db.Posts
                    .Include(p => p.User) // userul care a facut postarea
                    .Include(p => p.Reactions)
                    .Include(p => p.Comments.OrderByDescending(c => c.Date))
                       .ThenInclude(c => c.User) // userii care au scris comentariile
                    .Where(p => p.Id == id)
                    .FirstOrDefault();

            if (post is null)
            {
                return NotFound();
            }

            SetAccessRights();

            return View(post);
        }


        // metoda ce afiseaza formularul in care se vor completa datele unei postari
        // toti utilizatorii inregistrati pot adauga postari

        [Authorize(Roles = "User,Admin")]
        public IActionResult New()
        {
            Post post = new Post();

            return View(post);
        }

        // metoda ce adauga efectiv postarea
        [HttpPost]
        [Authorize(Roles = "User,Admin")]

        // programare asincrona -> adaugarea fisierului pe disc poate dura
        // -> ca sa nu blocam restul aplicatiei
        public async Task<IActionResult> New(Post post, IFormFile? Media)
        {

            // preluam id-ul utilizatorului care face postarea
            post.UserId = _userManager.GetUserId(User);
            post.Date = DateTime.Now;

            // verificam daca textul postarii este adecvat folosind companionul ai
            if (!string.IsNullOrEmpty(post.Text))
            {
                var filterResult = await _contentFilter.IsContentSafeAsync(post.Text);

                // daca textul contine limbaj nepotrivit, oprim publicarea
                if (filterResult.Success && !filterResult.IsAppropriate)
                        {
                            // folosim tempdata pentru a transmite eroarea catre pagina de index dupa redirect
                            TempData["PostErrorMessage"] = "Your content contains inappropriate terms. Please rephrase.";
                            return RedirectToAction("Index");
                        }
            }

            if (Media != null && Media.Length > 0)
            {
                // lista cu extensiile permise
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".mp4", ".mov" };

                // retine extensia fisierului pe care l-am incarcat
                var fileExtension = Path.GetExtension(Media.FileName).ToLower();

                // verificam extensia
                if (!allowedExtensions.Contains(fileExtension))
                {
                    // daca fisierul nu are extensia potrivita -> nu este fisier sau video
                    ModelState.AddModelError("Media", "Fișierul trebuie să fie o imagine(jpg, jpeg, png, gif) sau un video (mp4, mov).");
                    return View(post);
                }

                // calea de stocare a fisierului incarcat -> in wwwroot -> folderul images
                var storagePath = Path.Combine(_env.WebRootPath, "images", Media.FileName);


                var databaseFileName = "/images/" + Media.FileName;

                // FileStream -> "deschidem" un stream catre hard-disk la locatia "storagePath"
                // FileMode.Create -> spune ca vrem sa cream un fisier nou ( sau sa il suprascriem daca exisita )
                using (var fileStream = new FileStream(storagePath, FileMode.Create))
                {
                    // copiaza bitii din memoria RAM (unde sta fisierul temporar) pe hard-disk
                    await Media.CopyToAsync(fileStream);
                }

                // curatam erorile de validare pentru campul Media
                ModelState.Remove(nameof(post.Media));
                // salvam in baza de date calea catre imagine (nu toata poza) -> browser-ul o poate citi mai tarziu
                post.Media = databaseFileName;
            }

            if (ModelState.IsValid)
            {
                // se adauga postarea in baza de date
                db.Posts.Add(post);
                db.SaveChanges();

                // se redirectioneaza inapoi la Index
                return RedirectToAction("Index");
            }

            else
            {
                return RedirectToAction("Index");
            }
        }


        // se afiseaza formularul pentru editare
        // utilizatorii pot edita postarile lor

        [Authorize(Roles = "User,Admin")]
        public IActionResult Edit(int id)
        {
            // se cauta postarea cu id ul dat
            Post? post = db.Posts
                        .Where(p => p.Id == id)
                        .FirstOrDefault();

            if (post is null)
            {
                return NotFound();
            }

            // se verifica daca user ul ce vrea se editeze postarea are rolul necesar
            if (post.UserId == _userManager.GetUserId(User))
            {
                return View(post);
            }
            else
            {

                TempData["message"] = "You do not have the rights to edit posts you do not own.";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }
        }


        // actiunea ce adauga postarea modificata in baza de date

        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        // am transformat metoda in asincrona pentru a putea folosi companionul ai
        public async Task<IActionResult> Edit(int id, Post requestPost)
        {
            Post? post = db.Posts.Find(id);

            if (post is null)
            {
                return NotFound();
            }
            else
            {
                // verificam continutul modificat prin ai inainte de salvare\
                if (!string.IsNullOrEmpty(requestPost.Text))
                {
                    var filterResult = await _contentFilter.IsContentSafeAsync(requestPost.Text);

                    // daca textul contine limbaj nepotrivit, oprim publicarea
                    if (filterResult.Success && !filterResult.IsAppropriate)
                    {
                        // adaugam o eroare pe model pentru a fi afisata in formular
                        ModelState.AddModelError("Text", "Your content contains inappropriate terms. Please rephrase.");
                        return View(requestPost);
                    }
                }

                if (ModelState.IsValid)
                {
                    if (post.UserId == _userManager.GetUserId(User))
                    {
                        post.Text = requestPost.Text;
                        post.Media = requestPost.Media;
                        db.SaveChanges();

                        return RedirectToAction("Index");
                    }
                    else
                    {
                        return RedirectToAction("Index");
                    }
                }
                else
                {
                    return View(requestPost);
                }
            }
        }

        // actiunea ce sterge o postare din baza de date


        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public ActionResult Delete(int id)
        {

            Post? post = db.Posts.Where(p => p.Id == id)
                                 .FirstOrDefault();

            if (post is null)
            {
                return NotFound();
            }

            else
            {
                if ((post.UserId == _userManager.GetUserId(User)) || User.IsInRole("Admin"))
                {
                    // se sterge postarea din baza de date
                    db.Posts.Remove(post);
                    db.SaveChanges();
                    TempData["message"] = "Postarea a fost stearsa!";
                    TempData["messageType"] = "alert-success";

                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["message"] = "Nu aveti dreptul sa stergeti o postare care nu va apartine";
                    TempData["messageType"] = "alert-danger";

                    return RedirectToAction("Index");
                }
            }
        }

        // adaugarea unui comentariu asociat unei postari in baza de date
        // atat utilizatorii, cat si administratorii pot adauga comentarii

        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        // am facut metoda asincrona pentru verificarea ai a comentariului
        public async Task<IActionResult> Show([FromForm] Comment comment)
        {

            // se preia id-ul user ului care posteaza comentariul
            comment.UserId = _userManager.GetUserId(User);

            // trimitem comentariul catre ai pentru a valida limbajul
            if (!string.IsNullOrEmpty(comment.Content))
            {
                var filterResult = await _contentFilter.IsContentSafeAsync(comment.Content);

                if (filterResult.Success && !filterResult.IsAppropriate)
                {
                    // daca e neadecvat, trimitem un mesaj de eroare prin TempData si dam redirect inapoi
                    TempData["message"] = "Your content contains inappropriate terms. Please rephrase.";
                    TempData["messageType"] = "alert-danger";
                    return Redirect("/Posts/Show/" + comment.PostId);
                }
            }

            if (ModelState.IsValid)
            {
                // se adauga comentariul in baza de date
                db.Comments.Add(comment);
                db.SaveChanges();
                return Redirect("/Posts/Show/" + comment.PostId);
            }
            else
            {
                // daca validarea esuaeaza este nevoie sa se reafiseze pagina originala (Show.cshtml)
                // cu mesajele de eroare vizibile -> trebuie creat din nou Modelul Post complet 
                Post? post = db.Posts
                                .Include(p => p.User)
                                .Include(p => p.Comments.OrderByDescending(c => c.Date))
                                    .ThenInclude(c => c.User)
                                .Where(post => post.Id == comment.PostId)
                                .FirstOrDefault();

                if (post is null)
                {
                    return NotFound();
                }

                SetAccessRights();

                return View(post);
            }

        }

        // metoda pentru conditiile de afisare a butoanelor de editare si stergere
        private void SetAccessRights()
        {
            ViewBag.AfisareButoane = false;

            // verificam daca userul este autentificat
            if (User.IsInRole("User"))
            {
                ViewBag.AfisareButoane = true;
            }

            // extragem id-ul user ului curent
            ViewBag.UserCurent = _userManager.GetUserId(User);

            // verificam daca user ul este in rolul de admin
            ViewBag.EsteAdmin = User.IsInRole("Admin");
        }

        // actiunea pentru reactie (spark) - toggle
        // doar userii autentificati pot lasa reactii la postarii
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ToggleReaction(int postId)
        {
            var userId = _userManager.GetUserId(User);

            // verificam daca am reactionat deja la aceasta postare
            var reaction = await db.Reactions.FirstOrDefaultAsync(r => r.PostId == postId
                                                                    && r.UserId == userId);
            bool isReacted = false;

            if (reaction != null)
            {
                // daca exista deja reactia, o stergem
                db.Reactions.Remove(reaction);
                isReacted = false;
            }
            else
            {
                // daca nu exista, o adaugam
                var newReaction = new Reaction
                {
                    PostId = postId,
                    UserId = userId,
                    ReactionType = "Spark"
                };
                db.Reactions.Add(newReaction);
                isReacted = true;
            }
            await db.SaveChangesAsync();

            // numaram cate reactii are postarea acum
            var reactionCnt = await db.Reactions.CountAsync(r => r.PostId == postId);

            return Json(new { success = true, isReacted = isReacted, count = reactionCnt });
        }


        // adaugarea unui comentariu -> rapid
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddComment(int postId, string content)
        {
            // validare simpla pentru continutul comentariului
            if (string.IsNullOrWhiteSpace(content))
            {
                return Json(new { success = false, message = "The comment cannot be empty." });
            }

            // apelam filtrul ai pentru comentariul rapid
            var filterResult = await _contentFilter.IsContentSafeAsync(content);
            if (filterResult.Success && !filterResult.IsAppropriate)
            {
                // returnam eroarea direct in json pentru a fi afisata prietenos in interfata
                return Json(new { success = false, message = "Your content contains inappropriate terms. Please rephrase." });
            }

            var userId = _userManager.GetUserId(User);
            // luam userul complet ca sa ii putem trimite numele si poza inapoi in JSON
            var user = await db.Users.FindAsync(userId);

            var comment = new Comment
            {
                PostId = postId,
                UserId = userId,
                Content = content,
                Date = DateTime.Now
            };

            db.Comments.Add(comment);
            await db.SaveChangesAsync();

            // returnam JSON cu datele comentariului pentru a-l adauga in pagina imediat
            return Json(new
            {
                success = true,
                id = comment.Id, // trebuie sa returnam si id-ul ca sa stie edit / delete ce comentariu editam / stergem fara sa dam refresh
                userName = user.FirstName + " " + user.LastName,
                userImage = user.ProfilePicture ?? "/images/default-avatar.png",
                date = comment.Date.ToString("MMM dd • HH:mm"),
                content = comment.Content
            });
        }

        // actiune pentru cautare live cu dropdown
        [HttpGet]
        public async Task<IActionResult> SearchUsers(string query)
        {
            // daca nu scrie nimic, nu returnam nimic
            if (string.IsNullOrWhiteSpace(query))
                return Ok();

            var currentUserId = _userManager.GetUserId(User);

            // cautam useri dupa nume, prenume sau combinatia dintre cele doua
            var users = await db.Users
                .Where(u => (u.FirstName.Contains(query) ||
                            u.LastName.Contains(query) ||
                            (u.FirstName + " " + u.LastName).Contains(query)) &&
                            u.Id != currentUserId) // sa nu se afiseze si user ul curent
                .Take(5) // luam doar primii 5 ca sa nu fie lista prea lunga
                .ToListAsync();

            // returnam partial view-ul creat mai sus
            return PartialView("UserSearchDropdown", users);
        }

    }

}