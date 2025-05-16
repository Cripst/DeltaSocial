using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class ProfileController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // Căutare persoane
    public async Task<IActionResult> Search(string query)
    {
        var results = await _context.Profiles
            .Where(p => p.Visibility == "Public" && p.Name.Contains(query))
            .ToListAsync();

        return View(results);
    }

    // Vizualizare profil
    public async Task<IActionResult> ViewProfile(int id)
    {
        var profile = await _context.Profiles
            .Include(p => p.Posts)
            .Include(p => p.Albums)
            .ThenInclude(a => a.Photos)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (profile == null) return NotFound();

        var userId = _userManager.GetUserId(User);
        if (profile.Visibility == "Private" && profile.UserId != userId)
            return Forbid();

        return View(profile);
    }

    // Creare postare
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreatePost(string content)
    {
        var userId = _userManager.GetUserId(User);
        var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
            return BadRequest("Profilul nu a fost găsit.");

        var post = new Post
        {
            Content = content,
            ProfileId = profile.Id
        };

        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        return RedirectToAction("ViewProfile", new { id = profile.Id });
    }
}
