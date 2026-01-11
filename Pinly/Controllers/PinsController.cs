using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pinly.Models;
using Pinly.Services; // <--- Namespace necesar
using Pinly.ViewModels;

namespace Pinly.Controllers
{
    [Authorize]
    public class PinsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AiCompanionService _ai; // <--- Serviciul AI

        public PinsController(AppDbContext context,
                              IWebHostEnvironment webHostEnvironment,
                              UserManager<ApplicationUser> userManager,
                              AiCompanionService ai) // <--- Injectare in constructor
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
            _ai = ai;
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(524288000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 524288000)]
        public async Task<IActionResult> Create(PinCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                // --- VERIFICARE AI (Titlu si Descriere) ---
                if (!await _ai.IsSafe(model.Title) || !await _ai.IsSafe(model.Description))
                {
                    ModelState.AddModelError(string.Empty, "Titlul sau descrierea contin termeni nepotriviti.");
                    return View(model);
                }

                string uniqueFileName = null;
                if (model.Image != null)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "pins");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                    uniqueFileName = Guid.NewGuid().ToString() + "_" + model.Image.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.Image.CopyToAsync(fileStream);
                    }
                }

                var user = await _userManager.GetUserAsync(User);
                var pin = new Pin
                {
                    Title = model.Title,
                    Description = model.Description,
                    MediaPath = "/images/pins/" + uniqueFileName,
                    CreatedDate = DateTime.Now,
                    ApplicationUserId = user.Id
                };
                _context.Pins.Add(pin);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "Home");
            }
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Show(int id)
        {
            var pin = await _context.Pins
                .Include(p => p.ApplicationUser)
                .Include(p => p.Reactions)
                .Include(p => p.Comments).ThenInclude(c => c.ApplicationUser)
                .Include(p => p.Comments).ThenInclude(c => c.Likes)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (pin == null) return NotFound();
            return View(pin);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int pinId, string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return RedirectToAction("Show", new { id = pinId });

            // --- VERIFICARE AI (Comentariu) ---
            if (!await _ai.IsSafe(content))
            {
                TempData["Error"] = "Comentariul tau contine limbaj nepotrivit.";
                return RedirectToAction("Show", new { id = pinId });
            }

            var user = await _userManager.GetUserAsync(User);
            var comment = new Comment
            {
                PinId = pinId,
                Content = content,
                CreatedDate = DateTime.Now,
                ApplicationUserId = user.Id
            };
            _context.Comments.Add(comment);

            var pin = await _context.Pins.FindAsync(pinId);
            if (pin != null && pin.ApplicationUserId != user.Id)
            {
                _context.Notifications.Add(new Notification
                {
                    SenderId = user.Id,
                    RecipientId = pin.ApplicationUserId,
                    PinId = pinId,
                    Type = "Comment",
                    Text = "a comentat la pin-ul tau.",
                    CreatedDate = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Show", new { id = pinId });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var pin = await _context.Pins.FindAsync(id);
            if (pin == null) return NotFound();
            var user = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            if (pin.ApplicationUserId != user.Id && !isAdmin) return Forbid();

            if (!string.IsNullOrEmpty(pin.MediaPath))
            {
                var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, pin.MediaPath.TrimStart('/'));
                if (System.IO.File.Exists(imagePath)) System.IO.File.Delete(imagePath);
            }
            _context.Pins.Remove(pin);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int commentId)
        {
            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null) return NotFound();
            var user = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            if (comment.ApplicationUserId != user.Id && !isAdmin) return Forbid();
            var pinId = comment.PinId;
            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
            return RedirectToAction("Show", new { id = pinId });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditComment(int commentId, string newContent)
        {
            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null) return NotFound();
            var user = await _userManager.GetUserAsync(User);
            if (comment.ApplicationUserId != user.Id) return Forbid();

            if (!string.IsNullOrWhiteSpace(newContent))
            {
                // --- VERIFICARE AI (Editare Comentariu) ---
                if (!await _ai.IsSafe(newContent))
                {
                    TempData["Error"] = "Comentariul modificat contine limbaj nepotrivit.";
                    return RedirectToAction("Show", new { id = comment.PinId });
                }

                comment.Content = newContent;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Show", new { id = comment.PinId });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLike(int pinId, string returnUrl)
        {
            var user = await _userManager.GetUserAsync(User);
            var pin = await _context.Pins.FindAsync(pinId);
            if (pin == null) return NotFound();

            var reaction = await _context.Reactions.FirstOrDefaultAsync(r => r.PinId == pinId && r.ApplicationUserId == user.Id);
            if (reaction != null)
            {
                _context.Reactions.Remove(reaction);
                var oldNotif = await _context.Notifications.FirstOrDefaultAsync(n => n.Type == "Like" && n.SenderId == user.Id && n.PinId == pinId);
                if (oldNotif != null) _context.Notifications.Remove(oldNotif);
            }
            else
            {
                _context.Reactions.Add(new Reaction { PinId = pinId, ApplicationUserId = user.Id });
                if (pin.ApplicationUserId != user.Id)
                {
                    _context.Notifications.Add(new Notification
                    {
                        SenderId = user.Id,
                        RecipientId = pin.ApplicationUserId,
                        PinId = pinId,
                        Type = "Like",
                        Text = "a apreciat pin-ul tau.",
                        CreatedDate = DateTime.Now
                    });
                }
            }
            await _context.SaveChangesAsync();
            if (!string.IsNullOrEmpty(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCommentLike(int commentId)
        {
            var user = await _userManager.GetUserAsync(User);
            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null) return NotFound();

            var like = await _context.CommentLikes
                .FirstOrDefaultAsync(cl => cl.CommentId == commentId && cl.ApplicationUserId == user.Id);

            if (like != null)
            {
                _context.CommentLikes.Remove(like);
                var oldNotif = await _context.Notifications.FirstOrDefaultAsync(n => n.Type == "CommentLike" && n.SenderId == user.Id && n.RecipientId == comment.ApplicationUserId && n.PinId == comment.PinId);
                if (oldNotif != null) _context.Notifications.Remove(oldNotif);
            }
            else
            {
                _context.CommentLikes.Add(new CommentLike { CommentId = commentId, ApplicationUserId = user.Id });
                if (comment.ApplicationUserId != user.Id)
                {
                    _context.Notifications.Add(new Notification
                    {
                        SenderId = user.Id,
                        RecipientId = comment.ApplicationUserId,
                        PinId = comment.PinId,
                        Type = "CommentLike",
                        Text = "a apreciat comentariul tau.",
                        CreatedDate = DateTime.Now
                    });
                }
            }
            await _context.SaveChangesAsync();
            return RedirectToAction("Show", new { id = comment.PinId });
        }

        [HttpGet]
        public async Task<IActionResult> Likers(int id)
        {
            var pin = await _context.Pins
                .Include(p => p.Reactions).ThenInclude(r => r.ApplicationUser)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (pin == null) return NotFound();
            return View(pin);
        }
    }
}