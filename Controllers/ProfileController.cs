using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

[Authorize]
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
        ViewBag.SearchQuery = query; // Păstrăm termenul de căutare pentru a-l afișa înapoi în formular
        ViewBag.SearchPerformed = true; // Indicăm că s-a efectuat o căutare

        var results = await _context.Profiles
            .Where(p => p.Visibility == "Public" && (string.IsNullOrEmpty(query) || p.Name.Contains(query)))
            .ToListAsync();

        // Returnăm vizualizarea Search cu lista de rezultate
        return View("Search", results);
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

    // Creare album
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateAlbum(string title)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound("User not found.");
        }

        var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null)
        {
            return BadRequest("Profilul nu a fost găsit.");
        }

        var album = new Album
        {
            Title = title,
            ProfileId = profile.Id
        };

        _context.Albums.Add(album);
        await _context.SaveChangesAsync();

        // Redirecționăm înapoi la pagina de profil
        return RedirectToAction("Index", "Profile");
    }

    // Vizualizare detalii album
    [Authorize]
    [HttpGet("ViewAlbum/{id}")] // Specificăm ruta cu ID-ul albumului
    public async Task<IActionResult> ViewAlbum(int id)
    {
        var album = await _context.Albums
            .Include(a => a.Photos)
            .Include(a => a.Profile) // Include profilul pentru a verifica vizibilitatea/proprietarul
            .FirstOrDefaultAsync(a => a.Id == id);

        if (album == null)
        {
            return NotFound("Album not found.");
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var user = await _userManager.FindByIdAsync(userId);

        // Verificăm dacă utilizatorul are permisiunea să vadă albumul
        // (fie este public, fie utilizatorul este proprietarul profilului)
        if (album.Profile.Visibility == "Private" && (user == null || album.Profile.UserId != user.Id))
        {
            return Forbid(); // Returnăm 403 Forbidden dacă nu are permisiunea
        }

        // Transmitem obiectul album către vizualizarea ViewAlbum
        return View(album);
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            // Acest caz nu ar trebui să se întâmple datorită atributului [Authorize],
            // dar este bine să avem o verificare.
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            // Utilizatorul autentificat din token nu a fost găsit în baza de date
            return NotFound("User not found.");
        }

        // Folosim ProfileId-ul utilizatorului pentru a găsi profilul
        var profile = await _context.Profiles
            .Include(p => p.Posts)
            .Include(p => p.Albums)
            .ThenInclude(a => a.Photos)
            .FirstOrDefaultAsync(p => p.Id == user.ProfileId);

        if (profile == null)
        {
            // Profilul nu a fost găsit pentru utilizator (deși ar trebui creat la înregistrare)
            return NotFound("Profile not found.");
        }

        // Transmitem obiectul profile către vizualizare
        return View(profile);
    }
}
