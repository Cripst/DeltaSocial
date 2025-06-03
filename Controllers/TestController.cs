using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace DeltaSocial.Controllers
{
    [Route("[controller]")]
    public class TestController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<TestController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;

        public TestController(
            UserManager<ApplicationUser> userManager, 
            IHttpClientFactory clientFactory,
            ILogger<TestController> logger,
            ApplicationDbContext context,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _clientFactory = clientFactory;
            _logger = logger;
            _context = context;
            _signInManager = signInManager;
            _configuration = configuration;
        }

        // MVC Routes
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            try
            {
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation($"User registered successfully. UserId: {user.Id}");

                    // Creăm profilul pentru utilizator
                    var profile = new Profile
                    {
                        UserId = user.Id,
                        Name = model.Email.Split('@')[0], // Folosim partea din email înainte de @ ca nume
                        Visibility = "Public"
                    };

                    _context.Profiles.Add(profile);
                    await _context.SaveChangesAsync();

                    // Actualizăm utilizatorul cu ID-ul profilului
                    user.ProfileId = profile.Id;
                    await _userManager.UpdateAsync(user);

                    // Autentificăm utilizatorul imediat după înregistrare
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    // Redirecționăm către pagina de profil după înregistrare reușită
                    return RedirectToAction("Index", "Profile");
                }

                // Dacă înregistrarea eșuează, adăugăm erorile în ModelState
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                // Revenim la vizualizarea Index (unde ar fi formularul de înregistrare)
                return View("Index", model);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during registration: {ex.Message}");
                // Adăugăm eroarea în ModelState și revenim la vizualizare
                ModelState.AddModelError(string.Empty, "A aparut o eroare la inregistrare.");
                return View("Index", model);
            }
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginModel model)
        {
            try
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, isPersistent: false, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    // Redirecționăm către pagina de profil după login reușit
                    return RedirectToAction("Index", "Profile");
                }

                // Dacă login-ul eșuează, adăugăm o eroare în ModelState
                ModelState.AddModelError(string.Empty, "Login invalid.");

                // Revenim la vizualizarea Index (unde ar fi formularul de login)
                return View("Index", model);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during login: {ex.Message}");
                // Adăugăm eroarea în ModelState și revenim la vizualizare
                ModelState.AddModelError(string.Empty, "A aparut o eroare la autentificare.");
                return View("Index", model);
            }
        }

        [HttpGet("Profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                {
                    return Unauthorized(new { message = "User not found in token" });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found in database" });
                }

                var profile = await _context.Profiles
                    .Include(p => p.Posts)
                    .Include(p => p.Albums)
                    .ThenInclude(a => a.Photos)
                    .FirstOrDefaultAsync(p => p.Id == user.ProfileId);

                if (profile == null)
                {
                    return NotFound(new { message = "Profile not found" });
                }

                return Ok(new
                {
                    id = profile.Id,
                    name = profile.Name,
                    visibility = profile.Visibility,
                    posts = profile.Posts?.Select(p => new { id = p.Id, content = p.Content, created = p.CreatedAt }),
                    albums = profile.Albums?.Select(a => new { id = a.Id, title = a.Title, photos = a.Photos?.Count })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting profile: {ex.Message}");
                return BadRequest(new { error = ex.Message });
            }
        }

        // API Routes
        [HttpGet("api/public")]
        public IActionResult GetPublicData()
        {
            return Ok(new { message = "This is public data! Anyone can access it." });
        }

        [HttpGet("api/protected")]
        [Authorize]
        public async Task<IActionResult> GetProtectedData()
        {
            try
            {
                // Verificăm toate claim-urile din token
                _logger.LogInformation("All claims in token:");
                foreach (var claim in User.Claims)
                {
                    _logger.LogInformation($"Claim: {claim.Type} = {claim.Value}");
                }

                // Încercăm să găsim ID-ul din diferite claim-uri
                var nameIdClaims = User.FindAll(ClaimTypes.NameIdentifier).ToList();
                var email = User.FindFirst(ClaimTypes.Email)?.Value;

                _logger.LogInformation($"Found {nameIdClaims.Count} name identifier claims");
                foreach (var claim in nameIdClaims)
                {
                    _logger.LogInformation($"NameId claim: {claim.Value}");
                }

                // Încercăm să găsim utilizatorul după email
                if (!string.IsNullOrEmpty(email))
                {
                    var userByEmail = await _userManager.FindByEmailAsync(email);
                    if (userByEmail != null)
                    {
                        _logger.LogInformation($"User found by email. ID: {userByEmail.Id}");
                        return Ok(new
                        {
                            message = "This is protected data!",
                            user = new
                            {
                                id = userByEmail.Id,
                                email = userByEmail.Email,
                                username = userByEmail.UserName
                            }
                        });
                    }
                }

                // Încercăm să găsim utilizatorul după ID (verificăm toate claim-urile nameidentifier)
                foreach (var claim in nameIdClaims)
                {
                    // Verificăm dacă claim-ul este un GUID valid
                    if (Guid.TryParse(claim.Value, out _))
                    {
                        var userById = await _userManager.FindByIdAsync(claim.Value);
                        if (userById != null)
                        {
                            _logger.LogInformation($"User found by ID. Email: {userById.Email}");
                            return Ok(new
                            {
                                message = "This is protected data!",
                                user = new
                                {
                                    id = userById.Id,
                                    email = userById.Email,
                                    username = userById.UserName
                                }
                            });
                        }
                    }
                }

                _logger.LogWarning("User not found in database");
                return NotFound(new { message = "User not found in database" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in protected endpoint: {ex.Message}");
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
