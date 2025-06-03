using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace DeltaSocial.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _context = context;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { message = "Auth controller is working!" });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            try
            {
                // Create user with both UserName and Email set to the email
                var user = new ApplicationUser 
                { 
                    UserName = model.Email,
                    Email = model.Email,
                    NormalizedUserName = model.Email.ToUpperInvariant(),
                    NormalizedEmail = model.Email.ToUpperInvariant(),
                    EmailConfirmed = true // Since we're not using email confirmation for now
                };
                
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Create a profile for the new user
                    var profile = new Profile
                    {
                        UserId = user.Id,
                        Name = model.Email.Split('@')[0], // Use part of email as name
                        Visibility = "Public"
                    };
                    _context.Profiles.Add(profile);
                    await _context.SaveChangesAsync();

                    // Update user with profile
                    user.ProfileId = profile.Id;
                    await _userManager.UpdateAsync(user);

                    // Add user to the "User" role
                    await _userManager.AddToRoleAsync(user, "User");

                    return Ok(new { message = "Registration successful" });
                }

                return BadRequest(new { errors = result.Errors });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            try
            {
                // Try to find user by email
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    return Unauthorized(new { message = "Invalid email or password" });
                }

                // Check if email is confirmed
                if (!user.EmailConfirmed)
                {
                    return Unauthorized(new { message = "Please confirm your email before logging in" });
                }

                // Verify password directly using UserManager
                var passwordValid = await _userManager.CheckPasswordAsync(user, model.Password);
                if (!passwordValid)
                {
                    return Unauthorized(new { message = "Invalid email or password" });
                }

                // Load user with profile
                user = await _context.Users
                    .Include(u => u.Profile)
                    .FirstOrDefaultAsync(u => u.Id == user.Id);

                return Ok(new { 
                    message = "Login successful",
                    user = new {
                        id = user.Id,
                        email = user.Email,
                        profileId = user.ProfileId,
                        profileName = user.Profile?.Name
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }

    public class RegisterModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class LoginModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
} 