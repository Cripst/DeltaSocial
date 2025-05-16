using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Security.Claims;

namespace DeltaSocial.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly UserManager<ApplicationUser> _userManager;

        public TestController(IHttpClientFactory clientFactory, UserManager<ApplicationUser> userManager)
        {
            _clientFactory = clientFactory;
            _userManager = userManager;
        }

        [HttpGet("protected")]
        [Authorize]
        public async Task<IActionResult> GetProtectedData()
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

                return Ok(new
                {
                    message = "This is protected data!",
                    user = new
                    {
                        id = user.Id,
                        email = user.Email,
                        username = user.UserName
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("public")]
        public IActionResult GetPublicData()
        {
            return Ok(new { message = "This is public data! Anyone can access it." });
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string email, string password)
        {
            var client = _clientFactory.CreateClient();
            var content = new StringContent(
                JsonSerializer.Serialize(new { email, password }),
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync("https://localhost:7236/api/auth/register", content);
            var result = await response.Content.ReadAsStringAsync();

            ViewBag.Message = result;
            return View("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var client = _clientFactory.CreateClient();
            var content = new StringContent(
                JsonSerializer.Serialize(new { email, password }),
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync("https://localhost:7236/api/auth/login", content);
            var result = await response.Content.ReadAsStringAsync();

            ViewBag.Message = result;
            return View("Index");
        }
    }
}
