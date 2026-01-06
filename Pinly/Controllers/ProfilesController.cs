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

        public ProfilesController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Profiles/Show/user_id_aici
        [HttpGet]
        public async Task<IActionResult> Show(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            // 1. Găsim utilizatorul pe care vrem să îl vedem
            var userProfile = await _userManager.FindByIdAsync(id);
            if (userProfile == null)
            {
                return NotFound();
            }

            // 2. Îi luăm pin-urile
            var pins = await _context.Pins
                .Where(p => p.ApplicationUserId == id)
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync();

            // 3. Verificăm dacă cel care se uită este proprietarul contului
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
    }
}