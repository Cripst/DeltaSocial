using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;

public class FriendshipController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public FriendshipController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [Authorize]
    public async Task<IActionResult> FriendRequests()
    {
        var userId = _userManager.GetUserId(User);
        var requests = await _context.Friendships
            .Include(f => f.Sender)
            .Include(f => f.Receiver)
            .Where(f => (f.SenderId == userId || f.ReceiverId == userId) && f.Status == "Pending")
            .ToListAsync();
            
        return View(requests);
    }

    [Authorize]
    public async Task<IActionResult> SendRequest(string receiverId)
    {
        var senderId = _userManager.GetUserId(User);

        if (senderId == receiverId)
        {
            TempData["Error"] = "You cannot send a friend request to yourself.";
            return RedirectToAction("Index", "Home");
        }

        var exists = await _context.Friendships.AnyAsync(f =>
            (f.SenderId == senderId && f.ReceiverId == receiverId) ||
            (f.SenderId == receiverId && f.ReceiverId == senderId));

        if (!exists)
        {
            var friendship = new Friendship
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Status = "Pending"
            };
            _context.Friendships.Add(friendship);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Friend request sent successfully!";
        }
        else
        {
            TempData["Error"] = "A friend request already exists between these users.";
        }

        return RedirectToAction("ViewProfile", "Profile", new { id = receiverId });
    }

    [Authorize]
    public async Task<IActionResult> RespondRequest(int id, bool accept)
    {
        var request = await _context.Friendships
            .Include(f => f.Sender)
            .Include(f => f.Receiver)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (request != null && request.ReceiverId == _userManager.GetUserId(User))
        {
            request.Status = accept ? "Accepted" : "Rejected";
            await _context.SaveChangesAsync();
            
            TempData["Success"] = accept ? 
                $"You are now friends with {request.Sender.UserName}!" : 
                $"You have rejected {request.Sender.UserName}'s friend request.";
        }

        return RedirectToAction("FriendRequests");
    }
}
