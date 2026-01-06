using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Necesar pentru .Include si .ToListAsync
using Pinly.Models;
using System.Diagnostics;

namespace Pinly.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context; // Adaugam contextul bazei de date

        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Preluam pin-urile, includem autorul si le ordonam descrescator dupa data
            var pins = await _context.Pins
                .Include(p => p.ApplicationUser)
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