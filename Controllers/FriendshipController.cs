using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

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
    public async Task<IActionResult> SendRequest(string receiverId)
    {
        var senderId = _userManager.GetUserId(User);

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
        }

        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    public async Task<IActionResult> RespondRequest(int id, bool accept)
    {
        var request = await _context.Friendships.FindAsync(id);

        if (request != null && request.ReceiverId == _userManager.GetUserId(User))
        {
            request.Status = accept ? "Accepted" : "Rejected";
            await _context.SaveChangesAsync();
        }

        return RedirectToAction("Index", "Home");
    }
}
