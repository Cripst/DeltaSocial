using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Logging;

namespace DeltaSocial.Controllers
{
    [Route("[controller]")]
    public class TestController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<TestController> _logger;

        public TestController(
            UserManager<ApplicationUser> userManager, 
            IHttpClientFactory clientFactory,
            ILogger<TestController> logger)
        {
            _userManager = userManager;
            _clientFactory = clientFactory;
            _logger = logger;
        }

        // MVC Routes
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(string email, string password)
        {
            try
            {
                var user = new ApplicationUser { UserName = email, Email = email };
                var result = await _userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    _logger.LogInformation($"User registered successfully. UserId: {user.Id}");
                    ViewBag.Message = $"Registration successful. UserId: {user.Id}";
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError($"Registration failed: {errors}");
                    ViewBag.Message = errors;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Registration error: {ex.Message}");
                ViewBag.Message = $"Error: {ex.Message}";
            }

            return View("Index");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(string email, string password)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    _logger.LogWarning($"Login failed: User not found for email {email}");
                    ViewBag.Message = "Invalid email or password";
                    return View("Index");
                }

                _logger.LogInformation($"User found. UserId: {user.Id}");
                var result = await _userManager.CheckPasswordAsync(user, password);
                if (result)
                {
                    ViewBag.Message = $"Login successful. UserId: {user.Id}";
                }
                else
                {
                    ViewBag.Message = "Invalid email or password";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Login error: {ex.Message}");
                ViewBag.Message = $"Error: {ex.Message}";
            }

            return View("Index");
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
