using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using DeltaSocial.Attributes;
using System.Threading.Tasks;

namespace DeltaSocial.Controllers
{
    [Authorize]
    [VisitorAccess]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProfileController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet]
        public async Task<IActionResult> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return View(new List<Profile>());

            var profiles = await _context.Profiles
                .Where(p => p.Visibility == "Public" && 
                           (p.Name.Contains(query) || p.UserId.Contains(query)))
                .ToListAsync();

            return View(profiles);
        }

        [HttpGet]
        public async Task<IActionResult> View(string id)
        {
            var profile = await _context.Profiles
                .Include(p => p.Posts)
                .Include(p => p.Albums)
                .FirstOrDefaultAsync(p => p.UserId == id);

            if (profile == null)
                return NotFound();

            // Check if profile is private
            if (profile.Visibility == "Private")
            {
                // If user is not authenticated, return unauthorized
                if (!User.Identity.IsAuthenticated)
                    return Unauthorized();

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                    return Unauthorized();

                // Check if users are friends
                var friendship = await _context.Friendships
                    .FirstOrDefaultAsync(f => 
                        (f.SenderId == currentUser.Id && f.ReceiverId == id && f.Status == "Accepted") ||
                        (f.SenderId == id && f.ReceiverId == currentUser.Id && f.Status == "Accepted"));

                if (friendship == null && currentUser.Id != id)
                    return Unauthorized();
            }

            return View(profile);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UpdateVisibility(string visibility)
        {
            if (visibility != "Public" && visibility != "Private")
                return BadRequest("Invalid visibility setting");

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var profile = await _context.Profiles
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (profile == null)
                return NotFound();

            profile.Visibility = visibility;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(View), new { id = user.Id });
        }

        // Creare postare
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreatePost(string title, string content)
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
                return BadRequest("Title and content are required");

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var profile = await _context.Profiles
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (profile == null)
                return NotFound();

            var post = new Post
            {
                Title = title,
                Content = content,
                ProfileId = profile.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(View), new { id = user.Id });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> DeletePost(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var post = await _context.Posts
                .Include(p => p.Profile)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null)
                return NotFound();

            // Check if user is the post owner or a moderator
            if (post.Profile.UserId != user.Id && !User.IsInRole("Moderator"))
                return Forbid();

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(View), new { id = post.Profile.UserId });
        }

        // Creare album
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateAlbum(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return BadRequest("Title is required");

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var profile = await _context.Profiles
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (profile == null)
                return NotFound();

            var album = new Album
            {
                Title = title,
                ProfileId = profile.Id
            };

            _context.Albums.Add(album);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(View), new { id = user.Id });
        }

        // Adaugare poza la album
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AddPhotoToAlbum(int albumId, IFormFile photoFile)
        {
            if (photoFile == null || photoFile.Length == 0)
                return BadRequest("No file uploaded");

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var album = await _context.Albums
                .Include(a => a.Profile)
                .FirstOrDefaultAsync(a => a.Id == albumId);

            if (album == null)
                return NotFound();

            if (album.Profile.UserId != user.Id)
                return Forbid();

            // Generate a unique filename
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(photoFile.FileName)}";
            var filePath = Path.Combine("wwwroot", "uploads", "photos", fileName);

            // Ensure the directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            // Save the file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await photoFile.CopyToAsync(stream);
            }

            var photo = new Photo
            {
                Url = $"/uploads/photos/{fileName}",
                AlbumId = albumId
            };

            _context.Photos.Add(photo);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(ViewAlbum), new { id = albumId });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> DeletePhoto(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var photo = await _context.Photos
                .Include(p => p.Album)
                .ThenInclude(a => a.Profile)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (photo == null)
                return NotFound();

            // Check if user is the photo owner or a moderator
            if (photo.Album.Profile.UserId != user.Id && !User.IsInRole("Moderator"))
                return Forbid();

            // Delete the file
            var filePath = Path.Combine("wwwroot", photo.Url.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            _context.Photos.Remove(photo);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(ViewAlbum), new { id = photo.AlbumId });
        }

        [HttpGet]
        public async Task<IActionResult> ViewAlbum(int id)
        {
            var album = await _context.Albums
                .Include(a => a.Profile)
                .Include(a => a.Photos)
                .ThenInclude(p => p.Comments)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (album == null)
                return NotFound();

            // Check if profile is private
            if (album.Profile.Visibility == "Private")
            {
                // If user is not authenticated, return unauthorized
                if (!User.Identity.IsAuthenticated)
                    return Unauthorized();

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                    return Unauthorized();

                // Check if users are friends
                var friendship = await _context.Friendships
                    .FirstOrDefaultAsync(f => 
                        (f.SenderId == currentUser.Id && f.ReceiverId == album.Profile.UserId && f.Status == "Accepted") ||
                        (f.SenderId == album.Profile.UserId && f.ReceiverId == currentUser.Id && f.Status == "Accepted"));

                if (friendship == null && currentUser.Id != album.Profile.UserId)
                    return Unauthorized();
            }

            return View(album);
        }

        public async Task<IActionResult> Index()
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

            // First try to find the profile by UserId
            var profile = await _context.Profiles
                .Include(p => p.Posts)
                .Include(p => p.Albums)
                .ThenInclude(a => a.Photos)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            // If no profile found by UserId, try by ProfileId
            if (profile == null && user.ProfileId.HasValue)
            {
                profile = await _context.Profiles
                    .Include(p => p.Posts)
                    .Include(p => p.Albums)
                    .ThenInclude(a => a.Photos)
                    .FirstOrDefaultAsync(p => p.Id == user.ProfileId.Value);
            }

            // If still no profile found, create one
            if (profile == null)
            {
                profile = new Profile
                {
                    UserId = userId,
                    Name = user.Email.Split('@')[0],
                    Visibility = "Public"
                };

                _context.Profiles.Add(profile);
                await _context.SaveChangesAsync();

                // Update the user with the new profile ID
                user.ProfileId = profile.Id;
                await _userManager.UpdateAsync(user);
            }

            return View(profile);
        }
    }
}
