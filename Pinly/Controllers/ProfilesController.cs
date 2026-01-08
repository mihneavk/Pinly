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
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProfilesController(AppDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet]
        public async Task<IActionResult> Show(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var userProfile = await _context.Users
                .Include(u => u.Followers).ThenInclude(f => f.Follower)
                .Include(u => u.Following).ThenInclude(f => f.Followee)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (userProfile == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            bool isOwner = currentUser != null && currentUser.Id == userProfile.Id;
            bool isProfileAdmin = await _userManager.IsInRoleAsync(userProfile, "Admin");

            bool isAdminVisitor = currentUser != null && await _userManager.IsInRoleAsync(currentUser, "Admin");

            bool isFollowing = false;
            bool hasPendingRequest = false;

            if (currentUser != null)
            {
                var followRelation = userProfile.Followers.FirstOrDefault(f => f.FollowerId == currentUser.Id);
                if (followRelation != null)
                {
                    if (followRelation.IsAccepted) isFollowing = true;
                    else hasPendingRequest = true;
                }
            }

            // Logica private
            bool isLocked = userProfile.IsPrivate && !isFollowing && !isOwner && !isAdminVisitor;

            List<Pin> pins = new List<Pin>();
            List<ApplicationUser> followersList = new List<ApplicationUser>();
            List<ApplicationUser> followingList = new List<ApplicationUser>();
            List<ApplicationUser> pendingList = new List<ApplicationUser>();

            if (!isLocked)
            {
                pins = await _context.Pins
                    .Where(p => p.ApplicationUserId == id)
                    .OrderByDescending(p => p.CreatedDate)
                    .ToListAsync();

                followersList = userProfile.Followers.Where(f => f.IsAccepted).Select(f => f.Follower).ToList();
                followingList = userProfile.Following.Where(f => f.IsAccepted).Select(f => f.Followee).ToList();
            }

            if (isOwner)
            {
                pendingList = userProfile.Followers.Where(f => !f.IsAccepted).Select(f => f.Follower).ToList();

                if (isLocked)
                {
                    pins = await _context.Pins.Where(p => p.ApplicationUserId == id).OrderByDescending(p => p.CreatedDate).ToListAsync();
                    followersList = userProfile.Followers.Where(f => f.IsAccepted).Select(f => f.Follower).ToList();
                    followingList = userProfile.Following.Where(f => f.IsAccepted).Select(f => f.Followee).ToList();
                }
            }

            var model = new ProfileViewModel
            {
                User = userProfile,
                Pins = pins,
                IsOwner = isOwner,
                IsFollowing = isFollowing,
                HasPendingRequest = hasPendingRequest,
                IsProfileAdmin = isProfileAdmin,
                Followers = followersList,
                Following = followingList,
                PendingRequests = pendingList
            };

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePrivacy()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (await _userManager.IsInRoleAsync(user, "Admin")) return Forbid();

            user.IsPrivate = !user.IsPrivate;
            await _userManager.UpdateAsync(user);

            return RedirectToAction("Show", new { id = user.Id });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFollow(string userId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();
            if (currentUser.Id == userId) return BadRequest("Nu te poti urmari singur");

            if (await _userManager.IsInRoleAsync(currentUser, "Admin")) return Forbid();

            var targetUser = await _userManager.FindByIdAsync(userId);
            if (targetUser == null) return NotFound();

            if (await _userManager.IsInRoleAsync(targetUser, "Admin")) return BadRequest("Nu poti urmari adminul");

            var existingFollow = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == currentUser.Id && f.FolloweeId == targetUser.Id);

            if (existingFollow != null)
            {
                _context.Follows.Remove(existingFollow);
            }
            else
            {
                var newFollow = new Follow
                {
                    FollowerId = currentUser.Id,
                    FolloweeId = targetUser.Id,
                    IsAccepted = !targetUser.IsPrivate
                };
                _context.Follows.Add(newFollow);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction("Show", new { id = userId });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptRequest(string followerId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var follow = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FolloweeId == currentUser.Id);

            if (follow != null)
            {
                follow.IsAccepted = true;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Show", new { id = currentUser.Id });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeclineRequest(string followerId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var follow = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FolloweeId == currentUser.Id);

            if (follow != null)
            {
                _context.Follows.Remove(follow);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Show", new { id = currentUser.Id });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UpdateProfilePicture(IFormFile profilePicture)
        {
            if (profilePicture != null && profilePicture.Length > 0)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return NotFound();

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(profilePicture.FileName);
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "profiles");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create)) { await profilePicture.CopyToAsync(stream); }

                user.ProfilePicturePath = "/images/profiles/" + fileName;
                await _userManager.UpdateAsync(user);
            }
            return RedirectToAction("Show", new { id = _userManager.GetUserId(User) });
        }

        // --- METODA NOUA: Actualizare Descriere ---
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateDescription(string description)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // Actualizam descrierea (poate fi goala daca utilizatorul o sterge)
            user.Description = description;

            await _userManager.UpdateAsync(user);
            return RedirectToAction("Show", new { id = user.Id });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var pins = await _context.Pins.Where(p => p.ApplicationUserId == userId).ToListAsync();
            foreach (var pin in pins)
            {
                if (!string.IsNullOrEmpty(pin.MediaPath))
                {
                    var path = Path.Combine(_webHostEnvironment.WebRootPath, pin.MediaPath.TrimStart('/'));
                    if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                }
            }

            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded) return RedirectToAction("Index", "Home");
            return RedirectToAction("Show", new { id = userId });
        }

        // --- API Search Users ---
        [HttpGet]
        public async Task<IActionResult> SearchUsers(string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return Json(new List<object>());

            var users = await _context.Users
                .Where(u => u.UserName.Contains(term))
                .Select(u => new { id = u.Id, username = u.UserName, avatar = u.ProfilePicturePath })
                .Take(5)
                .ToListAsync();

            return Json(users);
        }
    }
}