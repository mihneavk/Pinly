using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pinly.Models;

namespace Pinly.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationsController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetLatest()
        {
            var userId = _userManager.GetUserId(User);

            var notifs = await _context.Notifications
                .Include(n => n.Sender)
                .Where(n => n.RecipientId == userId)
                .OrderByDescending(n => n.CreatedDate)
                .Take(10)
                .Select(n => new {
                    id = n.Id,
                    text = n.Text,
                    type = n.Type,
                    senderName = n.Sender.UserName,
                    senderPic = n.Sender.ProfilePicturePath,
                    senderId = n.SenderId,
                    pinId = n.PinId,
                    groupId = n.GroupId, // --- Asta este linia importanta ---
                    isRead = n.IsRead,
                    date = n.CreatedDate.ToString("dd MMM HH:mm")
                })
                .ToListAsync();

            return Json(notifs);
        }

        [HttpPost]
        public async Task<IActionResult> MarkRead(int id)
        {
            var notif = await _context.Notifications.FindAsync(id);
            if (notif != null && notif.RecipientId == _userManager.GetUserId(User))
            {
                notif.IsRead = true;
                await _context.SaveChangesAsync();
            }
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = _userManager.GetUserId(User);
            var unreadNotifs = await _context.Notifications
                .Where(n => n.RecipientId == userId && !n.IsRead)
                .ToListAsync();

            if (unreadNotifs.Any())
            {
                unreadNotifs.ForEach(n => n.IsRead = true);
                await _context.SaveChangesAsync();
            }
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> AcceptRequest(int notificationId)
        {
            var notif = await _context.Notifications.FindAsync(notificationId);
            var currentUser = await _userManager.GetUserAsync(User);

            if (notif == null || notif.RecipientId != currentUser.Id) return BadRequest();

            var follow = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == notif.SenderId && f.FolloweeId == currentUser.Id);

            if (follow != null)
            {
                follow.IsAccepted = true;
            }

            _context.Notifications.Remove(notif);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> DeclineRequest(int notificationId)
        {
            var notif = await _context.Notifications.FindAsync(notificationId);
            var currentUser = await _userManager.GetUserAsync(User);

            if (notif == null || notif.RecipientId != currentUser.Id) return BadRequest();

            var follow = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == notif.SenderId && f.FolloweeId == currentUser.Id);

            if (follow != null)
            {
                _context.Follows.Remove(follow);
            }

            _context.Notifications.Remove(notif);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}