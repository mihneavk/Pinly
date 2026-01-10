using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pinly.Models;

namespace Pinly.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ILogger<HomeController> logger, AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var currentUserId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");

            var followingIds = new List<string>();
            if (currentUserId != null)
            {
                followingIds = await _context.Follows
                    .Where(f => f.FollowerId == currentUserId && f.IsAccepted)
                    .Select(f => f.FolloweeId)
                    .ToListAsync();
            }

            // Index arata: Conturi Publice + Proprii + Urmariti + Tot (daca esti Admin)
            var pins = await _context.Pins
                .Include(p => p.ApplicationUser)
                .Include(p => p.Reactions)
                .Where(p =>
                    !p.ApplicationUser.IsPrivate ||
                    p.ApplicationUser.Id == currentUserId ||
                    isAdmin ||
                    followingIds.Contains(p.ApplicationUserId)
                )
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync();

            return View(pins);
        }

        // --- PAGINA NOUA: DOAR CEI URMARITI ---
        [Authorize]
        public async Task<IActionResult> Following()
        {
            var userId = _userManager.GetUserId(User);

            // 1. Luam lista celor pe care ii urmarim (si cererea e acceptata)
            var followingIds = await _context.Follows
                .Where(f => f.FollowerId == userId && f.IsAccepted)
                .Select(f => f.FolloweeId)
                .ToListAsync();

            // 2. Luam pin-urile DOAR de la acesti useri
            var pins = await _context.Pins
                .Include(p => p.ApplicationUser)
                .Include(p => p.Reactions)
                .Where(p => followingIds.Contains(p.ApplicationUserId))
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync();

            return View(pins);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}