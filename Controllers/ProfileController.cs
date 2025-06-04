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
    [Route("Profile")]
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

        [HttpGet("")]
        [HttpGet("Index")]
        [HttpGet("Index/{userId}")]
        [HttpGet("Index/{userId?}")]
        public async Task<IActionResult> Index(string userId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId ?? currentUser.Id);
            if (user == null)
                return NotFound();

            var profile = await _context.Profiles
                .Include(p => p.Posts)
                .Include(p => p.Albums)
                .ThenInclude(a => a.Photos)
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (profile == null)
                return NotFound();

            // Get friendship status if viewing another user's profile
            if (userId != null && userId != currentUser.Id)
            {
                var friendship = await _context.Friendships
                    .FirstOrDefaultAsync(f => 
                        (f.SenderId == currentUser.Id && f.ReceiverId == userId) ||
                        (f.SenderId == userId && f.ReceiverId == currentUser.Id));
                ViewBag.Friendship = friendship;
            }

            // Get friends list if viewing own profile
            if (userId == null || userId == currentUser.Id)
            {
                var friends = await _context.Friendships
                    .Where(f => f.Status == "Accepted" && (f.SenderId == currentUser.Id || f.ReceiverId == currentUser.Id))
                    .Select(f => new
                    {
                        Id = f.SenderId == currentUser.Id ? f.ReceiverId : f.SenderId,
                        Email = f.SenderId == currentUser.Id ? f.Receiver.Email : f.Sender.Email,
                        FriendshipId = f.Id
                    })
                    .ToListAsync();
                ViewBag.Friends = friends;
            }

            return View(profile);
        }

        [HttpGet("Search")]
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

        [HttpGet("View/{id}")]
        public new async Task<IActionResult> View(string id)
        {
            var profile = await _context.Profiles
                .Include(p => p.Posts)
                .Include(p => p.Albums)
                    .ThenInclude(a => a.Photos)
                .FirstOrDefaultAsync(p => p.UserId == id);

            if (profile == null)
                return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                // Get friendship status
                var friendship = await _context.Friendships
                    .FirstOrDefaultAsync(f => 
                        (f.SenderId == currentUser.Id && f.ReceiverId == id) ||
                        (f.SenderId == id && f.ReceiverId == currentUser.Id));

                ViewBag.FriendshipStatus = friendship?.Status;
                ViewBag.FriendshipId = friendship?.Id;
                ViewBag.IsReceiver = friendship?.ReceiverId == currentUser.Id;
            }

            // Check if profile is private
            if (profile.Visibility == "Private")
            {
                // If user is not authenticated, return unauthorized
                if (!User.Identity.IsAuthenticated)
                    return Unauthorized();

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
        [HttpPost("UpdateVisibility")]
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
        [HttpPost("CreatePost")]
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
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(View), new { id = user.Id });
        }

        [Authorize]
        [HttpPost("DeletePost")]
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
        [HttpPost("CreateAlbum")]
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
        [HttpPost("AddPhotoToAlbum")]
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
        [HttpPost("DeletePhoto")]
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

        [HttpGet("ViewAlbum/{id}")]
        public async Task<IActionResult> ViewAlbum(int id)
        {
            var album = await _context.Albums
                .Include(a => a.Profile)
                .Include(a => a.Photos)
                .ThenInclude(p => p.Comments)
                    .ThenInclude(c => c.User)
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

        [Authorize]
        [HttpPost("SendFriendRequest")]
        public async Task<IActionResult> SendFriendRequest(string receiverId)
        {
            var sender = await _userManager.GetUserAsync(User);
            if (sender == null)
                return Unauthorized();

            // Check if a friendship already exists
            var existingFriendship = await _context.Friendships
                .FirstOrDefaultAsync(f => 
                    (f.SenderId == sender.Id && f.ReceiverId == receiverId) ||
                    (f.SenderId == receiverId && f.ReceiverId == sender.Id));

            if (existingFriendship != null)
                return BadRequest("A friend request already exists between these users");

            var friendship = new Friendship
            {
                SenderId = sender.Id,
                ReceiverId = receiverId,
                Status = "Pending"
            };

            _context.Friendships.Add(friendship);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(View), new { id = receiverId });
        }

        [Authorize]
        [HttpPost("AcceptFriendRequest")]
        public async Task<IActionResult> AcceptFriendRequest(int friendshipId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f => f.Id == friendshipId && f.ReceiverId == user.Id);

            if (friendship == null)
                return NotFound();

            if (friendship.Status != "Pending")
                return BadRequest("This friend request has already been processed");

            friendship.Status = "Accepted";
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(View), new { id = friendship.SenderId });
        }

        [Authorize]
        [HttpPost("RejectFriendRequest")]
        public async Task<IActionResult> RejectFriendRequest(int friendshipId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f => f.Id == friendshipId && f.ReceiverId == user.Id);

            if (friendship == null)
                return NotFound();

            if (friendship.Status != "Pending")
                return BadRequest("This friend request has already been processed");

            friendship.Status = "Rejected";
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(View), new { id = friendship.SenderId });
        }

        [Authorize]
        [HttpPost("AddPhotoComment")]
        public async Task<IActionResult> AddPhotoComment(int photoId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return BadRequest("Comment content is required");

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var photo = await _context.Photos.Include(p => p.Album).FirstOrDefaultAsync(p => p.Id == photoId);
            if (photo == null)
                return NotFound();

            var comment = new Comment
            {
                Content = content,
                UserId = user.Id,
                PostId = null, // Not a post comment
                ApprovalStatus = "Pending",
                CreatedAt = DateTime.UtcNow
            };
            if (photo.Comments == null) photo.Comments = new List<Comment>();
            photo.Comments.Add(comment);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ViewAlbum), new { id = photo.AlbumId });
        }

        [Authorize]
        [HttpPost("ApproveComment")]
        public async Task<IActionResult> ApproveComment(int commentId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();
            var comment = await _context.Comments.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == commentId);
            if (comment == null)
                return NotFound();
            // Find the photo that contains this comment
            var photo = await _context.Photos.Include(p => p.Album).ThenInclude(a => a.Profile)
                .FirstOrDefaultAsync(p => p.Comments.Any(c => c.Id == commentId));
            if (photo == null || photo.Album == null || photo.Album.Profile == null)
                return NotFound();
            if (photo.Album.Profile.UserId != user.Id)
                return Forbid();
            comment.ApprovalStatus = "Accepted";
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ViewAlbum), new { id = photo.AlbumId });
        }

        [Authorize]
        [HttpPost("RejectComment")]
        public async Task<IActionResult> RejectComment(int commentId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();
            var comment = await _context.Comments.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == commentId);
            if (comment == null)
                return NotFound();
            // Find the photo that contains this comment
            var photo = await _context.Photos.Include(p => p.Album).ThenInclude(a => a.Profile)
                .FirstOrDefaultAsync(p => p.Comments.Any(c => c.Id == commentId));
            if (photo == null || photo.Album == null || photo.Album.Profile == null)
                return NotFound();
            if (photo.Album.Profile.UserId != user.Id)
                return Forbid();
            comment.ApprovalStatus = "Rejected";
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ViewAlbum), new { id = photo.AlbumId });
        }
    }
}
