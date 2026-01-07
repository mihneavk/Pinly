using System.Diagnostics;
using Microsoft.AspNetCore.Identity; // Necesar pentru UserManager
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pinly.Models;

namespace Pinly.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager; // Injectam UserManager

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

            // 1. Aflam pe cine urmareste utilizatorul curent (doar cereri acceptate)
            var followingIds = new List<string>();
            if (currentUserId != null)
            {
                followingIds = await _context.Follows
                    .Where(f => f.FollowerId == currentUserId && f.IsAccepted)
                    .Select(f => f.FolloweeId)
                    .ToListAsync();
            }

            // 2. Interogare Pins cu Filtrare
            var pins = await _context.Pins
                .Include(p => p.ApplicationUser)
                .Include(p => p.Reactions)
                .Where(p =>
                    // Cazul 1: Contul este Public
                    !p.ApplicationUser.IsPrivate ||

                    // Cazul 2: Este propriul meu pin
                    p.ApplicationUser.Id == currentUserId ||

                    // Cazul 3: Sunt Admin (vad tot)
                    isAdmin ||

                    // Cazul 4: Il urmaresc pe autor (si mi-a acceptat cererea)
                    followingIds.Contains(p.ApplicationUserId)
                )
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