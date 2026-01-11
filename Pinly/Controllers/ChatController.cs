using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pinly.Models;
using Pinly.Services;
using Pinly.ViewModels;

namespace Pinly.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AiCompanionService _ai; // Serviciul AI

        public ChatController(AppDbContext context, UserManager<ApplicationUser> userManager, AiCompanionService ai)
        {
            _context = context;
            _userManager = userManager;
            _ai = ai;
        }

        // --- PAGINA PRINCIPALA CHAT ---
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

        // --- LISTA GRUPURI PUBLICE ---
        [HttpGet]
        public async Task<IActionResult> PublicGroups()
        {
            var userId = _userManager.GetUserId(User);
            var groups = await _context.Groups
                .Include(g => g.Memberships)
                .Where(g => g.IsPublic)
                .ToListAsync();

            ViewBag.UserId = userId;
            return View(groups);
        }

        // --- CERERE JOIN GRUP PUBLIC ---
        [HttpPost]
        public async Task<IActionResult> JoinGroup(int groupId)
        {
            var userId = _userManager.GetUserId(User);
            var group = await _context.Groups.Include(g => g.Memberships).FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null || !group.IsPublic) return NotFound();
            if (group.Memberships.Any(m => m.ApplicationUserId == userId)) return RedirectToAction("PublicGroups");

            _context.GroupMemberships.Add(new GroupMembership
            {
                GroupId = groupId,
                ApplicationUserId = userId,
                IsAccepted = false
            });

            // Notificam adminii
            var admins = group.Memberships.Where(m => m.IsModerator || m.ApplicationUserId == group.ModeratorId).ToList();
            foreach (var admin in admins)
            {
                _context.Notifications.Add(new Notification
                {
                    SenderId = userId,
                    RecipientId = admin.ApplicationUserId,
                    Type = "Group",
                    GroupId = groupId,
                    Text = $"cere să intre în grupul '{group.Name}'.",
                    CreatedDate = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Cerere trimisă! Așteaptă aprobarea.";
            return RedirectToAction("PublicGroups");
        }

        // --- ACCEPTARE MEMBRU ---
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
                _context.Notifications.Add(new Notification
                {
                    SenderId = uid,
                    RecipientId = userIdToAccept,
                    Type = "Group",
                    GroupId = groupId,
                    Text = $"ți-a acceptat cererea în grupul '{group.Name}'.",
                    CreatedDate = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index", new { groupId });
        }

        // --- REFUZARE MEMBRU ---
        [HttpPost]
        public async Task<IActionResult> DeclineJoin(int groupId, string userIdToDecline)
        {
            var group = await _context.Groups.Include(g => g.Memberships).FirstOrDefaultAsync(g => g.Id == groupId);
            var uid = _userManager.GetUserId(User);
            if (group == null) return NotFound();

            var me = group.Memberships.FirstOrDefault(m => m.ApplicationUserId == uid);
            if (group.ModeratorId != uid && (me == null || !me.IsModerator)) return Forbid();

            var target = group.Memberships.FirstOrDefault(m => m.ApplicationUserId == userIdToDecline);
            if (target != null)
            {
                _context.GroupMemberships.Remove(target);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index", new { groupId });
        }

        // --- TRIMITERE MESAJ (CU MODERARE AI) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(int groupId, string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return RedirectToAction("Index", new { groupId });

            // 1. Verificare AI
            if (!await _ai.IsSafe(content))
            {
                TempData["Error"] = "Conținutul tău conține termeni nepotriviți. Te rugăm să reformulezi.";
                return RedirectToAction("Index", new { groupId });
            }

            var userId = _userManager.GetUserId(User);
            var group = await _context.Groups.Include(g => g.Memberships).FirstOrDefaultAsync(g => g.Id == groupId);
            if (group == null) return NotFound();

            var myMem = group.Memberships.FirstOrDefault(m => m.ApplicationUserId == userId);
            if (myMem == null || !myMem.IsAccepted) return Forbid();

            // 2. Verificare Block (DM)
            if (group.IsPrivate && group.Memberships.Any(m => m.IsBlocked))
            {
                TempData["Error"] = "Conversație blocată.";
                return RedirectToAction("Index", new { groupId });
            }

            var msg = new GroupMessage { GroupId = groupId, SenderId = userId, Content = content, CreatedDate = DateTime.Now };
            _context.GroupMessages.Add(msg);

            var others = group.Memberships.Where(m => m.ApplicationUserId != userId && m.IsAccepted).ToList();
            foreach (var mem in others)
            {
                var txt = group.IsPrivate ? "mesaj nou." : $"mesaj în '{group.Name}'.";
                _context.Notifications.Add(new Notification { SenderId = userId, RecipientId = mem.ApplicationUserId, Type = "Message", GroupId = groupId, Text = txt, CreatedDate = DateTime.Now });
            }
            await _context.SaveChangesAsync();
            return RedirectToAction("Index", new { groupId });
        }

        // --- CREARE GRUP (CU MODERARE AI) ---
        [HttpPost]
        public async Task<IActionResult> CreateGroup(string groupName, string description, bool isPublic)
        {
            if (string.IsNullOrWhiteSpace(groupName)) { TempData["Error"] = "Nume obligatoriu."; return RedirectToAction("Index"); }

            // 1. Verificare AI pentru Nume/Descriere
            if (!await _ai.IsSafe(groupName) || !await _ai.IsSafe(description))
            {
                TempData["Error"] = "Numele sau descrierea conțin termeni nepotriviți.";
                return RedirectToAction("Index");
            }

            var userId = _userManager.GetUserId(User);

            var group = new Group
            {
                Name = groupName,
                Description = description,
                IsPrivate = false,
                IsPublic = isPublic,
                ModeratorId = userId,
                CreatedDate = DateTime.Now
            };
            _context.Groups.Add(group);
            await _context.SaveChangesAsync();

            _context.GroupMemberships.Add(new GroupMembership { GroupId = group.Id, ApplicationUserId = userId, IsModerator = true, IsAccepted = true });
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", new { groupId = group.Id });
        }

        // --- ADAUGARE MEMBRU (INVITE) ---
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

            if (!isAdmin && !isMod) { TempData["Error"] = "Nu ai permisiuni."; return RedirectToAction("Index", new { groupId }); }

            var userToAdd = await _userManager.FindByNameAsync(username);
            if (userToAdd == null) { TempData["Error"] = "Utilizator negăsit."; return RedirectToAction("Index", new { groupId }); }

            if (!group.Memberships.Any(m => m.ApplicationUserId == userToAdd.Id))
            {
                _context.GroupMemberships.Add(new GroupMembership { GroupId = groupId, ApplicationUserId = userToAdd.Id, IsAccepted = true });
                _context.Notifications.Add(new Notification { SenderId = currentUserId, RecipientId = userToAdd.Id, Type = "Group", GroupId = groupId, Text = $"te-a adăugat în '{group.Name}'.", CreatedDate = DateTime.Now });
                await _context.SaveChangesAsync();
                TempData["Success"] = "Membru adăugat.";
            }
            else { TempData["Error"] = "Utilizatorul este deja în grup."; }

            return RedirectToAction("Index", new { groupId });
        }

        // --- SCOATERE MEMBRU (KICK) ---
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

            if (targetIsAdmin) { TempData["Error"] = "Nu poți șterge administratorul."; return RedirectToAction("Index", new { groupId }); }

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

        // --- PARASIRE GRUP ---
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

        // --- BLOCK / UNBLOCK (DM) ---
        [HttpPost]
        public async Task<IActionResult> ToggleBlock(int groupId)
        {
            var uid = _userManager.GetUserId(User); var g = await _context.Groups.Include(x => x.Memberships).FirstOrDefaultAsync(x => x.Id == groupId);
            if (g != null && g.IsPrivate) { var m = g.Memberships.FirstOrDefault(x => x.ApplicationUserId == uid); if (m != null) { m.IsBlocked = !m.IsBlocked; await _context.SaveChangesAsync(); } }
            return RedirectToAction("Index", new { groupId });
        }

        // --- PROMOVARE / RETROGRADARE ---
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

        // --- EDITARE MESAJ (CU MODERARE AI) ---
        [HttpPost]
        public async Task<IActionResult> EditMessage(int messageId, string newContent)
        {
            // 1. Verificare AI
            if (!await _ai.IsSafe(newContent))
            {
                TempData["Error"] = "Limbaj nepotrivit.";
                var m_fail = await _context.GroupMessages.FindAsync(messageId);
                return RedirectToAction("Index", new { groupId = m_fail?.GroupId });
            }

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

        // --- CAUTARE GLOBALA ---
        [HttpGet]
        public async Task<IActionResult> SearchUsers(string term, int? groupId)
        {
            var currentUserId = _userManager.GetUserId(User);

            // 1. Luam Adminii pt excludere
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            var adminIds = admins.Select(a => a.Id).ToList();

            var query = _context.Users.AsQueryable();

            // 2. Excludem userul curent
            query = query.Where(u => u.Id != currentUserId);

            // 3. Excludem Adminii
            if (adminIds.Any())
            {
                query = query.Where(u => !adminIds.Contains(u.Id));
            }

            // 4. Daca suntem intr-un grup, excludem membrii existenti
            if (groupId.HasValue)
            {
                var existingMemberIds = _context.GroupMemberships
                    .Where(gm => gm.GroupId == groupId.Value)
                    .Select(gm => gm.ApplicationUserId);

                query = query.Where(u => !existingMemberIds.Contains(u.Id));
            }

            // 5. Cautare dupa nume
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