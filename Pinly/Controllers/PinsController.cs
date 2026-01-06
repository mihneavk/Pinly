using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pinly.Models;
using Pinly.ViewModels;

namespace Pinly.Controllers
{
    [Authorize] // Doar utilizatorii logați pot accesa acest controller
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

                // 1. Procesare și salvare imagine pe server
                if (model.Image != null)
                {
                    // Calea către folderul wwwroot/images/pins
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "pins");

                    // Creăm folderul dacă nu există
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // Generăm nume unic (Guid) + extensia originală
                    uniqueFileName = Guid.NewGuid().ToString() + "_" + model.Image.FileName;

                    // Calea completă pe disc
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    // Copierea fișierului (Async)
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.Image.CopyToAsync(fileStream);
                    }
                }

                // 2. Preluare user curent
                var user = await _userManager.GetUserAsync(User);

                // 3. Mapare ViewModel -> Model de Bază de Date
                var pin = new Pin
                {
                    Title = model.Title,
                    Description = model.Description,
                    // Salvăm calea relativă pentru a o folosi în <img src="...">
                    MediaPath = "/images/pins/" + uniqueFileName,
                    CreatedDate = DateTime.Now,
                    ApplicationUserId = user.Id
                };

                // 4. Salvare în DB
                _context.Pins.Add(pin);
                await _context.SaveChangesAsync();

                // Redirect către prima pagină
                return RedirectToAction("Index", "Home");
            }

            // Dacă validarea eșuează, reafisăm formularul cu erori
            return View(model);
        }

        // GET: Afișează pagina unui Pin specific
        [HttpGet]
        [AllowAnonymous] // Lăsăm și oaspeții să vadă pinul, dar nu să comenteze
        public async Task<IActionResult> Show(int id)
        {
            var pin = await _context.Pins
                .Include(p => p.ApplicationUser) // Încărcăm autorul pinului
                .Include(p => p.Comments) // Încărcăm comentariile
                    .ThenInclude(c => c.ApplicationUser) // Încărcăm autorii comentariilor
                .FirstOrDefaultAsync(m => m.Id == id);

            if (pin == null)
            {
                return NotFound();
            }

            return View(pin);
        }

        // POST: Adaugă un comentariu
        [HttpPost]
        [Authorize] // Trebuie să fii logat ca să comentezi
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int pinId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                // Dacă comentariul e gol, reîncărcăm pagina
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

            // Ne întoarcem la aceeași pagină pentru a vedea comentariul
            return RedirectToAction("Show", new { id = pinId });
        }
    }
}