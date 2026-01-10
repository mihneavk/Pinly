using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pinly.Models;
using Pinly.ViewModels;

namespace Pinly.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ChatController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? groupId)
        {
            var userId = _userManager.GetUserId(User);

            var memberships = await _context.GroupMemberships
                .Include(gm => gm.Group).ThenInclude(g => g.Memberships).ThenInclude(m => m.ApplicationUser)
                .Include(gm => gm.Group).ThenInclude(g => g.Messages)
                .Where(gm => gm.ApplicationUserId == userId)
                .ToListAsync();

            var myGroups = memberships.Select(m => m.Group)
                .OrderByDescending(g => g.Messages.Any() ? g.Messages.Max(msg => msg.CreatedDate) : g.CreatedDate)
                .ToList();

            var model = new ChatViewModel
            {
                MyGroups = myGroups,
                CurrentGroupId = groupId,
                CurrentUserId = userId,
                Messages = new List<GroupMessage>()
            };

            if (groupId.HasValue)
            {
                var currentGroup = await _context.Groups
                    .Include(g => g.Messages).ThenInclude(m => m.Sender)
                    .Include(g => g.Memberships).ThenInclude(m => m.ApplicationUser)
                    .FirstOrDefaultAsync(g => g.Id == groupId.Value);

                if (currentGroup != null)
                {
                    var myMem = currentGroup.Memberships.FirstOrDefault(m => m.ApplicationUserId == userId);

                    if (myMem != null && myMem.IsAccepted)
                    {
                        model.CurrentGroup = currentGroup;
                        model.Messages = currentGroup.Messages.OrderBy(m => m.CreatedDate).ToList();

                        if (currentGroup.IsPrivate)
                        {
                            var other = currentGroup.Memberships.FirstOrDefault(m => m.ApplicationUserId != userId)?.ApplicationUser;
                            if (other != null) model.CurrentGroup.Name = other.UserName;
                        }
                    }
                    else if (myMem != null && !myMem.IsAccepted)
                    {
                        model.CurrentGroup = currentGroup;
                        ViewBag.IsPending = true;
                    }
                }
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> PublicGroups()
        {
            var userId = _userManager.GetUserId(User);
            var groups = await _context.Groups.Include(g => g.Memberships).Where(g => g.IsPublic).ToListAsync();
            ViewBag.UserId = userId;
            return View(groups);
        }

        [HttpPost]
        public async Task<IActionResult> JoinGroup(int groupId)
        {
            var userId = _userManager.GetUserId(User);
            var group = await _context.Groups.Include(g => g.Memberships).FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null || !group.IsPublic) return NotFound();
            if (group.Memberships.Any(m => m.ApplicationUserId == userId)) return RedirectToAction("PublicGroups");

            _context.GroupMemberships.Add(new GroupMembership { GroupId = groupId, ApplicationUserId = userId, IsAccepted = false });

            var admins = group.Memberships.Where(m => m.IsModerator || m.ApplicationUserId == group.ModeratorId).ToList();
            foreach (var admin in admins)
            {
                _context.Notifications.Add(new Notification { SenderId = userId, RecipientId = admin.ApplicationUserId, Type = "Group", GroupId = groupId, Text = $"cere sa intre in '{group.Name}'.", CreatedDate = DateTime.Now });
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Cerere trimisa.";
            return RedirectToAction("PublicGroups");
        }

        [HttpPost]
        public async Task<IActionResult> AcceptJoin(int groupId, string userIdToAccept)
        {
            var group = await _context.Groups.Include(g => g.Memberships).FirstOrDefaultAsync(g => g.Id == groupId);
            var uid = _userManager.GetUserId(User);
            if (group == null) return NotFound();

            var me = group.Memberships.FirstOrDefault(m => m.ApplicationUserId == uid);
            if (group.ModeratorId != uid && (me == null || !me.IsModerator)) return Forbid();

            var target = group.Memberships.FirstOrDefault(m => m.ApplicationUserId == userIdToAccept);
            if (target != null)
            {
                target.IsAccepted = true;
                _context.Notifications.Add(new Notification { SenderId = uid, RecipientId = userIdToAccept, Type = "Group", GroupId = groupId, Text = $"ti-a acceptat cererea in '{group.Name}'.", CreatedDate = DateTime.Now });
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index", new { groupId });
        }

        [HttpPost]
        public async Task<IActionResult> DeclineJoin(int groupId, string userIdToDecline)
        {
            var group = await _context.Groups.Include(g => g.Memberships).FirstOrDefaultAsync(g => g.Id == groupId);
            var uid = _userManager.GetUserId(User);
            if (group == null) return NotFound();

            var me = group.Memberships.FirstOrDefault(m => m.ApplicationUserId == uid);
            if (group.ModeratorId != uid && (me == null || !me.IsModerator)) return Forbid();

            var target = group.Memberships.FirstOrDefault(m => m.ApplicationUserId == userIdToDecline);
            if (target != null) { _context.GroupMemberships.Remove(target); await _context.SaveChangesAsync(); }
            return RedirectToAction("Index", new { groupId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(int groupId, string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return RedirectToAction("Index", new { groupId });
            var userId = _userManager.GetUserId(User);
            var group = await _context.Groups.Include(g => g.Memberships).FirstOrDefaultAsync(g => g.Id == groupId);
            if (group == null) return NotFound();

            var myMem = group.Memberships.FirstOrDefault(m => m.ApplicationUserId == userId);
            if (myMem == null || !myMem.IsAccepted) return Forbid();

            if (group.IsPrivate && group.Memberships.Any(m => m.IsBlocked)) { TempData["Error"] = "Blocat."; return RedirectToAction("Index", new { groupId }); }

            var msg = new GroupMessage { GroupId = groupId, SenderId = userId, Content = content, CreatedDate = DateTime.Now };
            _context.GroupMessages.Add(msg);

            var others = group.Memberships.Where(m => m.ApplicationUserId != userId && m.IsAccepted).ToList();
            foreach (var mem in others)
            {
                var txt = group.IsPrivate ? "mesaj nou." : $"mesaj in '{group.Name}'.";
                _context.Notifications.Add(new Notification { SenderId = userId, RecipientId = mem.ApplicationUserId, Type = "Message", GroupId = groupId, Text = txt, CreatedDate = DateTime.Now });
            }
            await _context.SaveChangesAsync();
            return RedirectToAction("Index", new { groupId });
        }

        [HttpPost]
        public async Task<IActionResult> CreateGroup(string groupName, string description, bool isPublic)
        {
            if (string.IsNullOrWhiteSpace(groupName)) { TempData["Error"] = "Nume obligatoriu."; return RedirectToAction("Index"); }
            var userId = _userManager.GetUserId(User);

            var group = new Group { Name = groupName, Description = description, IsPrivate = false, IsPublic = isPublic, ModeratorId = userId, CreatedDate = DateTime.Now };
            _context.Groups.Add(group);
            await _context.SaveChangesAsync();

            _context.GroupMemberships.Add(new GroupMembership { GroupId = group.Id, ApplicationUserId = userId, IsModerator = true, IsAccepted = true });
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", new { groupId = group.Id });
        }

        [HttpPost]
        public async Task<IActionResult> AddMember(int groupId, string username)
        {
            var group = await _context.Groups.Include(g => g.Memberships).FirstOrDefaultAsync(g => g.Id == groupId);
            if (group == null || group.IsPrivate) return BadRequest();

            // MODIFICARE: Blocheaza adaugarea manuala daca grupul este public
            if (group.IsPublic)
            {
                TempData["Error"] = "În grupurile publice nu poți adăuga membri manual. Ei trebuie să dea Join.";
                return RedirectToAction("Index", new { groupId });
            }

            var currentUserId = _userManager.GetUserId(User);
            var requester = group.Memberships.FirstOrDefault(m => m.ApplicationUserId == currentUserId);

            bool isAdmin = group.ModeratorId == currentUserId;
            bool isMod = requester != null && requester.IsModerator;

            if (!isAdmin && !isMod) { TempData["Error"] = "Fara permisiuni."; return RedirectToAction("Index", new { groupId }); }

            var userToAdd = await _userManager.FindByNameAsync(username);
            if (userToAdd == null) { TempData["Error"] = "User negasit."; return RedirectToAction("Index", new { groupId }); }

            if (!group.Memberships.Any(m => m.ApplicationUserId == userToAdd.Id))
            {
                _context.GroupMemberships.Add(new GroupMembership { GroupId = groupId, ApplicationUserId = userToAdd.Id, IsAccepted = true });
                _context.Notifications.Add(new Notification { SenderId = currentUserId, RecipientId = userToAdd.Id, Type = "Group", GroupId = groupId, Text = $"te-a adaugat in '{group.Name}'.", CreatedDate = DateTime.Now });
                await _context.SaveChangesAsync();
                TempData["Success"] = "Adaugat.";
            }
            return RedirectToAction("Index", new { groupId });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveMember(int groupId, string userIdToRemove)
        {
            var group = await _context.Groups.Include(g => g.Memberships).FirstOrDefaultAsync(g => g.Id == groupId);
            if (group == null) return NotFound();

            var uid = _userManager.GetUserId(User);
            var req = group.Memberships.FirstOrDefault(m => m.ApplicationUserId == uid);
            var target = group.Memberships.FirstOrDefault(m => m.ApplicationUserId == userIdToRemove);

            if (target == null) return RedirectToAction("Index", new { groupId });

            bool iamAdmin = group.ModeratorId == uid;
            bool iamMod = req != null && req.IsModerator;
            bool targetIsAdmin = group.ModeratorId == userIdToRemove;
            bool targetIsMod = target.IsModerator;

            if (targetIsAdmin) { TempData["Error"] = "Nu poti sterge adminul."; return RedirectToAction("Index", new { groupId }); }

            if (!iamAdmin)
            {
                if (!iamMod) { if (uid != userIdToRemove) return Forbid(); }
                else { if (targetIsMod || targetIsAdmin) return Forbid(); }
            }

            _context.GroupMemberships.Remove(target);
            await _context.SaveChangesAsync();
            if (uid == userIdToRemove) return RedirectToAction("Index");
            return RedirectToAction("Index", new { groupId });
        }

        [HttpPost]
        public async Task<IActionResult> LeaveGroup(int groupId)
        {
            var uid = _userManager.GetUserId(User); var g = await _context.Groups.Include(x => x.Memberships).FirstOrDefaultAsync(x => x.Id == groupId);
            if (g != null)
            {
                var m = g.Memberships.FirstOrDefault(x => x.ApplicationUserId == uid);
                if (m != null)
                {
                    if (g.ModeratorId == uid && g.Memberships.Count > 1) { TempData["Error"] = "Adminul nu poate pleca."; return RedirectToAction("Index", new { groupId }); }
                    _context.GroupMemberships.Remove(m);
                    if (g.Memberships.Count <= 1) _context.Groups.Remove(g);
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleBlock(int groupId)
        {
            var uid = _userManager.GetUserId(User); var g = await _context.Groups.Include(x => x.Memberships).FirstOrDefaultAsync(x => x.Id == groupId);
            if (g != null && g.IsPrivate) { var m = g.Memberships.FirstOrDefault(x => x.ApplicationUserId == uid); if (m != null) { m.IsBlocked = !m.IsBlocked; await _context.SaveChangesAsync(); } }
            return RedirectToAction("Index", new { groupId });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleModerator(int groupId, string userIdToToggle)
        {
            var g = await _context.Groups.Include(gm => gm.Memberships).FirstOrDefaultAsync(x => x.Id == groupId);
            if (g == null) return NotFound();
            if (g.ModeratorId != _userManager.GetUserId(User)) return Forbid();
            var t = g.Memberships.FirstOrDefault(m => m.ApplicationUserId == userIdToToggle);
            if (t != null && t.ApplicationUserId != g.ModeratorId) { t.IsModerator = !t.IsModerator; await _context.SaveChangesAsync(); }
            return RedirectToAction("Index", new { groupId });
        }

        [HttpPost]
        public async Task<IActionResult> EditMessage(int messageId, string newContent)
        {
            var m = await _context.GroupMessages.FindAsync(messageId);
            if (m != null && m.SenderId == _userManager.GetUserId(User) && !string.IsNullOrWhiteSpace(newContent))
            {
                m.Content = newContent; m.IsEdited = true; m.CreatedDate = DateTime.Now;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index", new { groupId = m.GroupId });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMessage(int messageId)
        {
            var m = await _context.GroupMessages.FindAsync(messageId);
            if (m != null && m.SenderId == _userManager.GetUserId(User))
            {
                m.Content = "Sters."; m.IsDeleted = true;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index", new { groupId = m.GroupId });
        }

        [HttpPost]
        public async Task<IActionResult> CreatePrivateChat(string targetUserId)
        {
            var uid = _userManager.GetUserId(User);
            if (uid == targetUserId) return RedirectToAction("Index");
            var g = await _context.Groups.Where(x => x.IsPrivate && x.Memberships.Any(m => m.ApplicationUserId == uid) && x.Memberships.Any(m => m.ApplicationUserId == targetUserId)).FirstOrDefaultAsync();
            if (g != null)
            {
                if (!g.Memberships.Any(m => m.ApplicationUserId == uid)) { _context.GroupMemberships.Add(new GroupMembership { GroupId = g.Id, ApplicationUserId = uid, IsAccepted = true }); await _context.SaveChangesAsync(); }
                return RedirectToAction("Index", new { groupId = g.Id });
            }
            var newG = new Group { Name = "DM", IsPrivate = true };
            _context.Groups.Add(newG);
            await _context.SaveChangesAsync();
            _context.GroupMemberships.Add(new GroupMembership { GroupId = newG.Id, ApplicationUserId = uid, IsAccepted = true });
            _context.GroupMemberships.Add(new GroupMembership { GroupId = newG.Id, ApplicationUserId = targetUserId, IsAccepted = true });
            await _context.SaveChangesAsync();
            return RedirectToAction("Index", new { groupId = newG.Id });
        }

        [HttpGet]
        public async Task<IActionResult> SearchUsers(string term, int? groupId)
        {
            var currentUserId = _userManager.GetUserId(User);
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            var adminIds = admins.Select(a => a.Id).ToList();

            var query = _context.Users.AsQueryable();
            query = query.Where(u => u.Id != currentUserId);

            if (adminIds.Any())
            {
                query = query.Where(u => !adminIds.Contains(u.Id));
            }

            if (groupId.HasValue)
            {
                var existingMemberIds = _context.GroupMemberships
                    .Where(gm => gm.GroupId == groupId.Value)
                    .Select(gm => gm.ApplicationUserId);

                query = query.Where(u => !existingMemberIds.Contains(u.Id));
            }

            if (!string.IsNullOrWhiteSpace(term))
            {
                query = query.Where(u => u.UserName.Contains(term));
            }

            var users = await query
                .Select(u => new
                {
                    username = u.UserName,
                    avatar = u.ProfilePicturePath
                })
                .Take(5)
                .ToListAsync();

            return Json(users);
        }
    }
}