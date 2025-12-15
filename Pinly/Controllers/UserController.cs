using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pinly.Data;
using Pinly.Models;
using System.Security.Claims; // Necesar pentru a citi ID-ul userului logat

namespace Pinly.Controllers
{
    public class UserController : Controller
    {
        private readonly AppDbContext _db;

        public UserController(AppDbContext db)
        {
            _db = db;
        }

        // GET: User/Search
        public async Task<IActionResult> Search(string? searchTerm)
        {
            var users = _db.Users.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                string lowerSearchTerm = searchTerm.ToLower();
                // Notă: .ToLower() direct în query poate să nu meargă pe toate SGBD-urile, 
                // dar pe SQL Server e ok de obicei. 
                users = users.Where(u => u.FullName.Contains(searchTerm) ||
                                         u.UserName.Contains(searchTerm));
            }

            return View(await users.ToListAsync());
        }

        // GET: User/Show/{id}
        [HttpGet] 
        public async Task<IActionResult> Show(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var userProfile = await _db.Users
                .Include(u => u.Pins)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (userProfile == null)
            {
                return NotFound();
            }


            // daca profilul este public - îl afișăm direct
            if (userProfile.IsPublic)
            {
                return View(userProfile);
            }

            // daca profilul este privat, verificăm dacă cel care se uita este proprietarul
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (User.Identity.IsAuthenticated && currentUserId == userProfile.Id)
            {
                return View(userProfile);
            }

            // daca profilul este privat și vizitatorul NU este proprietar
            // ascundem pin-urile și marcăm pentru View că e privat
            userProfile.Pins = new List<Pin>(); // golim lista de pin-uri pentru siguranta
            ViewBag.Message = "Acest cont este privat.";

            // returnam view-ul, dar în HTML vei verifica daca Pins e gol sau ViewBag.Message
            return View(userProfile);
        }
    }
}