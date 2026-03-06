using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProiectDotNet.Data;
using ProiectDotNet.Models;
using ProiectDotNet.Services;

namespace ProiectDotNet.Controllers
{
    public class CommentsController(ApplicationDbContext context,
                                   UserManager<ApplicationUser> userManager,
                                   RoleManager<IdentityRole> roleManager,
                                   IContentFilterService contentFilter) : Controller
    {
        private readonly ApplicationDbContext db = context;
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly RoleManager<IdentityRole> _roleManager = roleManager;
        private readonly IContentFilterService _contentFilter = contentFilter;


        // stergerea unui comentariu -> doar de userul ce l-a postat si admin
        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public IActionResult Delete(int id)
        {
            // cautam comentariul cu id-ul dat
            Comment? comm = db.Comments.Find(id);

            if (comm == null)
            {
                return NotFound();
            }

            else
            {
                // verificam daca cel ce vrea sa stearga comentariul este
                // user ul ce l-a postat sau admin
                if (comm.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
                {
                    // stergem comentariul si salvam modificarile din baza de date
                    db.Comments.Remove(comm);
                    db.SaveChanges();
                    // redirectionare catre pagina de unde a venit cererea

                    // vreau ca userul sa ramana pe aceeasi pagina dupa stergerea unui comentariu
                    // de asta am folosit referer

                    //string referer = Request.Headers["Referer"].ToString();
                    //return !string.IsNullOrEmpty(referer) ? Redirect(referer) : RedirectToAction("Index", "Posts");

                    return Json(new { success = true });
                }
                else
                {
                    // se redirectioneaza catre afisarea postarilor
                    return RedirectToAction("Index", "Posts");
                }
            }
        }

        //editarea unui comentariu -> doar de user ul ce l-a postat
        // [HttpGet] implicit
        [Authorize(Roles = "User")]


        public IActionResult Edit(int id)
        {
            // cautam comentariul cu id ul dat
            Comment? comm = db.Comments.Find(id);

            if (comm is null)
            {
                return NotFound();
            }
            else
            {
                // verificam daca cel ce vrea sa editeze comentariul este
                // user ul ce l-a postat sau admin
                if (comm.UserId == _userManager.GetUserId(User))
                {
                    return View(comm);
                }
                else
                {
                    TempData["message"] = "You do not have the rights to edit this comment.";
                    TempData["messageType"] = "alert-danger";
                    return RedirectToAction("Index", "Posts");

                }
            }
        }

        [HttpPost]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> Edit(int id, [FromForm] Comment requestComment)
        {
            Comment? comm = db.Comments.Find(id);

            if (comm is null)
            {
                return Json(new { success = false, message = "Comment not found." });
            }

            if (comm.UserId == _userManager.GetUserId(User))
            {
                if (!string.IsNullOrEmpty(requestComment.Content))
                {
                    // verificam daca comentariul contine termeni nepotriviti inainte de publicare
                    var filterResult = await _contentFilter.IsContentSafeAsync(requestComment.Content);
                    if (filterResult.Success && !filterResult.IsAppropriate)
                    {
                        // trebuie sa returnam rezultatele sub forma de json ca sa nu fie nevoie sa reincarcam pagina
                        return Json(new { success = false, message = "Your content contains inappropriate terms. Please rephrase." });
                    }
                }

                if (ModelState.IsValid)
                {
                    comm.Content = requestComment.Content;
                    db.SaveChanges();
                    return Json(new { success = true, content = comm.Content });
                }

                return Json(new { success = false, message = "Invalid data." });
            }

            return Json(new { success = false, message = "You do not have permission to edit this comment." });
        }
    }
}