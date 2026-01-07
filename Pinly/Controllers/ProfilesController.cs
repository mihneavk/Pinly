using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pinly.Models;
using Pinly.ViewModels;

namespace Pinly.Controllers
{
    public class ProfilesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment; // Necesar pentru a salva fișiere

        public ProfilesController(AppDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: /Profiles/Show/user_id_aici
        [HttpGet]
        public async Task<IActionResult> Show(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var userProfile = await _userManager.FindByIdAsync(id);
            if (userProfile == null)
            {
                return NotFound();
            }

            var pins = await _context.Pins
                .Where(p => p.ApplicationUserId == id)
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync();

            var currentUser = await _userManager.GetUserAsync(User);
            bool isOwner = currentUser != null && currentUser.Id == userProfile.Id;

            var model = new ProfileViewModel
            {
                User = userProfile,
                Pins = pins,
                IsOwner = isOwner
            };

            return View(model);
        }

        // POST: Upload Poza Profil
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UpdateProfilePicture(IFormFile profilePicture)
        {
            if (profilePicture != null && profilePicture.Length > 0)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return NotFound();

                // 1. Generăm un nume unic pentru fișier
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(profilePicture.FileName);

                // 2. Calea unde salvăm (wwwroot/images/profiles)
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "profiles");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var filePath = Path.Combine(uploadsFolder, fileName);

                // 3. Salvăm fișierul fizic
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profilePicture.CopyToAsync(stream);
                }

                // 4. Actualizăm calea în baza de date
                user.ProfilePicturePath = "/images/profiles/" + fileName;
                await _userManager.UpdateAsync(user);
            }

            // Ne întoarcem pe pagina de profil
            return RedirectToAction("Show", new { id = _userManager.GetUserId(User) });
        }
    }
}