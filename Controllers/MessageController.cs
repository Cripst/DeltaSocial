using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

public class MessageController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public MessageController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // Trimite mesaj prieten sau grup
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> SendMessage(string receiverId, string content, bool isGroup = false)
    {
        var senderId = _userManager.GetUserId(User);

        var message = new Message
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = content,
            IsGroupMessage = isGroup
        };

        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        return RedirectToAction("Inbox");
    }

    // Inbox: afișează mesajele primite și trimise
    [Authorize]
    public async Task<IActionResult> Inbox()
    {
        var userId = _userManager.GetUserId(User);

        var messages = await _context.Messages
            .Where(m => m.ReceiverId == userId || m.SenderId == userId)
            .OrderByDescending(m => m.SentAt)
            .ToListAsync();

        return View(messages);
    }
}
