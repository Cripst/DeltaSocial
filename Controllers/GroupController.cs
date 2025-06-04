using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DeltaSocial.Models;
using System.Threading.Tasks;
using System.Linq;

namespace DeltaSocial.Controllers
{
    [Authorize]
    public class GroupController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public GroupController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // List all groups
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var groups = await _context.Groups.Include(g => g.Members).ToListAsync();
            return View(groups);
        }

        // View a group and its members
        [AllowAnonymous]
        public async Task<IActionResult> View(int id)
        {
            var group = await _context.Groups.Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == id);
            if (group == null) return NotFound();
            return View(group);
        }

        // Join a group
        [HttpPost]
        public async Task<IActionResult> Join(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (profile == null) return NotFound();
            var group = await _context.Groups.Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == id);
            if (group == null) return NotFound();
            if (!group.Members.Contains(profile))
            {
                group.Members.Add(profile);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("View", new { id });
        }

        // Leave a group
        [HttpPost]
        public async Task<IActionResult> Leave(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (profile == null) return NotFound();
            var group = await _context.Groups.Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == id);
            if (group == null) return NotFound();
            if (group.Members.Contains(profile))
            {
                group.Members.Remove(profile);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        // Create a new group
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Group group)
        {
            if (ModelState.IsValid)
            {
                group.Members = new List<Profile>();
                _context.Groups.Add(group);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(group);
        }

        // Group chat page
        public async Task<IActionResult> Chat(int id)
        {
            var group = await _context.Groups.Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == id);
            if (group == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            var isMember = group.Members.Any(m => m.UserId == userId);
            if (!isMember)
            {
                return Forbid();
            }

            var messages = await _context.Messages
                .Where(m => m.GroupId == id)
                .OrderBy(m => m.CreatedAt)
                .Include(m => m.Sender)
                .ToListAsync();

            ViewBag.Group = group;
            return View(messages);
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage(int id, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return RedirectToAction("Chat", new { id });
            }
            var group = await _context.Groups.Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == id);
            if (group == null) return NotFound();
            var userId = _userManager.GetUserId(User);
            var isMember = group.Members.Any(m => m.UserId == userId);
            if (!isMember)
            {
                return Forbid();
            }
            var message = new Message
            {
                SenderId = userId,
                Content = content,
                GroupId = id,
                ReceiverId = null, // Ensure this is always null for group messages
                CreatedAt = DateTime.UtcNow
            };
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
            return RedirectToAction("Chat", new { id });
        }
    }
}
