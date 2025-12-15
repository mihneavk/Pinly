// Controllers/UserController.cs

using Microsoft.AspNetCore.Mvc;
using Pinly.Data; // Asigurați-vă că folosiți ApplicationDbContext
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Authorization; 

public class UserController : Controller
{
    private readonly ApplicationDbContext _db;

    public UserController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET
    public async Task<IActionResult> Search(string? searchTerm)
    {
        // 1. obtine toti utilizatorii
        var users = _db.Users.AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            // 2. Filtrare: cautare dupa parti din NumeComplet
            string lowerSearchTerm = searchTerm.ToLower();
            
            users = users.Where(u => u.NumeComplet.ToLower().Contains(lowerSearchTerm));
        }

        // Returneaza lista de utilizatori care se potrivesc
        return View(await users.ToListAsync());
    }
}

// GET: User/Show/{id}
public async Task<IActionResult> Show(string id)
{
    var user = await _db.Users
        .Include(u => u.Pins) // Include postările pentru afișarea conținutului complet
        .FirstOrDefaultAsync(u => u.Id == id);

    if (user == null)
    {
        return NotFound();
    }

    // 1. Verifică dacă profilul este Public
    if (user.IsPublic == true)
    {
        // Profilul este Public: Afișează TOATE informațiile (inclusiv Pins/Postări)
        return View(user);
    }
    
    // 2. Profilul este Privat (user.IsPublic == false)
    
    // Verifică dacă utilizatorul curent este PROPRIETARUL profilului
    // Proprietarii au dreptul de a vedea întotdeauna propriul profil privat
    if (ApplicationUser.Identity!.IsAuthenticated && ApplicationUser.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value == user.Id)
    {
        return View(user); // Proprietarul vede tot
    }
    
    // 3. Dacă nu e Public și nu e Proprietarul: Afișează doar informațiile de bază
    // Simulare funcționalitate tip Instagram - doar info de bază sunt vizibile
    
    // Crearea unui Model de bază pentru View (sau folosirea unui ViewBag)
    ViewBag.IsPrivate = true;
    
    // Restricționează colecția Pins la null
    user.Pins = null; 
    
    return View(user);
}