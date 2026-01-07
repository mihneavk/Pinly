using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pinly.Models;
using Pinly.ViewModels;

namespace Pinly.Controllers
{
    [Authorize]
    public class PinsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly UserManager<ApplicationUser> _userManager;

        public PinsController(AppDbContext context,
                              IWebHostEnvironment webHostEnvironment,
                              UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
        }

        // GET: Pins/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Pins/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PinCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                string uniqueFileName = null;

                if (model.Image != null)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "pins");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

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

        // GET: Show Pin
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Show(int id)
        {
            var pin = await _context.Pins
                .Include(p => p.ApplicationUser)
                .Include(p => p.Reactions) // Includem reactiile pentru numaratoare
                .Include(p => p.Comments)
                    .ThenInclude(c => c.ApplicationUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (pin == null)
            {
                return NotFound();
            }

            return View(pin);
        }

        // POST: Add Comment
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int pinId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
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
            await _context.SaveChangesAsync();

            return RedirectToAction("Show", new { id = pinId });
        }

        // POST: Delete Pin
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var pin = await _context.Pins.FindAsync(id);
            if (pin == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");

            if (pin.ApplicationUserId != user.Id && !isAdmin)
            {
                return Forbid();
            }

            if (!string.IsNullOrEmpty(pin.MediaPath))
            {
                var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, pin.MediaPath.TrimStart('/'));
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            _context.Pins.Remove(pin);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Home");
        }

        // POST: Delete Comment
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int commentId)
        {
            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");

            if (comment.ApplicationUserId != user.Id && !isAdmin)
            {
                return Forbid();
            }

            var pinId = comment.PinId;
            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            return RedirectToAction("Show", new { id = pinId });
        }

        // POST: Edit Comment
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditComment(int commentId, string newContent)
        {
            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);

            if (comment.ApplicationUserId != user.Id)
            {
                return Forbid();
            }

            if (!string.IsNullOrWhiteSpace(newContent))
            {
                comment.Content = newContent;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Show", new { id = comment.PinId });
        }

        // POST: Toggle Like
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLike(int pinId, string returnUrl)
        {
            var user = await _userManager.GetUserAsync(User);

            var reaction = await _context.Reactions
                .FirstOrDefaultAsync(r => r.PinId == pinId && r.ApplicationUserId == user.Id);

            if (reaction != null)
            {
                _context.Reactions.Remove(reaction);
            }
            else
            {
                var newReaction = new Reaction
                {
                    PinId = pinId,
                    ApplicationUserId = user.Id
                };
                _context.Reactions.Add(newReaction);
            }

            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        // GET: Likers List
        [HttpGet]
        public async Task<IActionResult> Likers(int id)
        {
            var pin = await _context.Pins
                .Include(p => p.Reactions)
                .ThenInclude(r => r.ApplicationUser)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pin == null) return NotFound();

            return View(pin);
        }
    }
}