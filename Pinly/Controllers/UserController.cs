using Microsoft.AspNetCore.Authorization; // --- NOU: Necesar pentru atributul [Authorize]
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

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool isFollowing = false;

            if (User.Identity.IsAuthenticated)
            {
                isFollowing = await _db.Follows
                    .AnyAsync(f => f.FollowerId == currentUserId && f.FolloweeId == id);
            }

            ViewBag.IsFollowing = isFollowing;
            ViewBag.FollowersCount = await _db.Follows.CountAsync(f => f.FolloweeId == id);


            if (userProfile.IsPublic)
            {
                return View(userProfile);
            }

            if (User.Identity.IsAuthenticated && currentUserId == userProfile.Id)
            {
                return View(userProfile);
            }

            userProfile.Pins = new List<Pin>();
            ViewBag.Message = "Acest cont este privat.";

            return View(userProfile);
        }

        [HttpPost]
        [Authorize] 
        public async Task<IActionResult> ToggleFollow(string id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (currentUserId == id) return BadRequest("Nu te poți urmări singur.");

            var existingFollow = await _db.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == currentUserId && f.FolloweeId == id);

            if (existingFollow != null)
            {
                // Unfollow
                _db.Follows.Remove(existingFollow);
            }
            else
            {
                // Follow
                var newFollow = new Follow
                {
                    FollowerId = currentUserId,
                    FolloweeId = id
                };
                _db.Follows.Add(newFollow);
            }

            await _db.SaveChangesAsync();
            return RedirectToAction("Show", new { id = id });
        }

        // GET: User/Followers/{id}
        [HttpGet]
        public async Task<IActionResult> Followers(string id)
        {
            var user = await _db.Users
                .Include(u => u.Followers)
                .ThenInclude(f => f.Follower) 
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return NotFound();

            ViewBag.TargetUserName = user.FullName;

            var followersList = user.Followers.Select(f => f.Follower).ToList();

            return View(followersList);
        }
    }
}