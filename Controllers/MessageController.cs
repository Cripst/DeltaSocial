using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DeltaSocial.Controllers
{
    [Authorize]
    public class MessageController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MessageController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            // Get all conversations (unique users the current user has exchanged messages with)
            var conversations = await _context.Messages
                .Where(m => m.SenderId == user.Id || m.ReceiverId == user.Id)
                .Select(m => m.SenderId == user.Id ? m.ReceiverId : m.SenderId)
                .Distinct()
                .ToListAsync();

            var conversationUsers = await _context.Users
                .Where(u => conversations.Contains(u.Id))
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    LastMessage = _context.Messages
                        .Where(m => (m.SenderId == user.Id && m.ReceiverId == u.Id) ||
                                  (m.SenderId == u.Id && m.ReceiverId == user.Id))
                        .OrderByDescending(m => m.CreatedAt)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return View(conversationUsers);
        }

        public async Task<IActionResult> Conversation(string userId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return Unauthorized();

            var otherUser = await _userManager.FindByIdAsync(userId);
            if (otherUser == null)
                return NotFound();

            // Check if users are friends
            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f => 
                    (f.SenderId == currentUser.Id && f.ReceiverId == userId && f.Status == "Accepted") ||
                    (f.SenderId == userId && f.ReceiverId == currentUser.Id && f.Status == "Accepted"));

            if (friendship == null)
                return Forbid();

            // Get all messages between the two users
            var messages = await _context.Messages
                .Where(m => (m.SenderId == currentUser.Id && m.ReceiverId == userId) ||
                           (m.SenderId == userId && m.ReceiverId == currentUser.Id))
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            // Mark unread messages as read
            foreach (var message in messages.Where(m => m.ReceiverId == currentUser.Id && !m.IsRead))
            {
                message.IsRead = true;
            }
            await _context.SaveChangesAsync();

            ViewBag.OtherUser = otherUser;
            return View(messages);
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage(string receiverId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return BadRequest("Message content is required");

            var sender = await _userManager.GetUserAsync(User);
            if (sender == null)
                return Unauthorized();

            // Check if users are friends
            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f => 
                    (f.SenderId == sender.Id && f.ReceiverId == receiverId && f.Status == "Accepted") ||
                    (f.SenderId == receiverId && f.ReceiverId == sender.Id && f.Status == "Accepted"));

            if (friendship == null)
                return Forbid();

            var message = new Message
            {
                SenderId = sender.Id,
                ReceiverId = receiverId,
                Content = content,
                GroupId = null // Ensure this is always null for private messages
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Conversation), new { userId = receiverId });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var message = await _context.Messages
                .FirstOrDefaultAsync(m => m.Id == id);

            if (message == null)
                return NotFound();

            // Only allow deletion of own messages
            if (message.SenderId != user.Id)
                return Forbid();

            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Conversation), new { userId = message.ReceiverId });
        }
    }
}
