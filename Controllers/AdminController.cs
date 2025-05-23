using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;
using DeltaSocial.Models;
using System.Linq;
using System.Collections.Generic;

[Authorize(Roles = "Moderator")]
public class AdminController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _context;

    public AdminController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users
            .Select(u => new UserViewModel
            {
                Id = u.Id,
                Email = u.Email,
                UserName = u.UserName,
                Roles = _userManager.GetRolesAsync(u).Result.ToList()
            })
            .ToListAsync();

        return View(users);
    }

    [HttpGet]
    public async Task<IActionResult> EditUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var userRoles = await _userManager.GetRolesAsync(user);
        var allRoles = await _roleManager.Roles
            .Where(r => r.Name != "Visitor")
            .ToListAsync();

        var model = new EditUserViewModel
        {
            Id = user.Id,
            Email = user.Email,
            UserName = user.UserName,
            UserRoles = userRoles.Where(r => r != "Visitor").ToList(),
            AllRoles = allRoles.Select(r => r.Name).ToList(),
            IsVisitor = userRoles.Contains("Visitor")
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> EditUser(EditUserViewModel model)
    {
        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null)
        {
            return NotFound();
        }

        // Get current roles
        var currentRoles = await _userManager.GetRolesAsync(user);

        // Inițializăm SelectedRoles dacă este null
        model.SelectedRoles ??= new List<string>();

        // Gestionăm rolul de Visitor separat
        if (model.IsVisitor && !currentRoles.Contains("Visitor"))
        {
            await _userManager.AddToRoleAsync(user, "Visitor");
        }
        else if (!model.IsVisitor && currentRoles.Contains("Visitor"))
        {
            await _userManager.RemoveFromRoleAsync(user, "Visitor");
        }

        // Gestionăm celelalte roluri
        foreach (var role in currentRoles.Where(r => r != "Visitor"))
        {
            if (!model.SelectedRoles.Contains(role))
            {
                await _userManager.RemoveFromRoleAsync(user, role);
            }
        }

        foreach (var role in model.SelectedRoles)
        {
            if (!currentRoles.Contains(role))
            {
                await _userManager.AddToRoleAsync(user, role);
            }
        }

        return RedirectToAction(nameof(Index));
    }

    // Șterge comentariu
    public async Task<IActionResult> DeleteComment(int id)
    {
        var comment = await _context.Comments.FindAsync(id);
        if (comment != null)
        {
            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction("Index", "Home");
    }

    // Șterge poză
    public async Task<IActionResult> DeletePhoto(int id)
    {
        var photo = await _context.Photos.FindAsync(id);
        if (photo != null)
        {
            _context.Photos.Remove(photo);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction("Index", "Home");
    }

    // Șterge mesaj
    public async Task<IActionResult> DeleteMessage(int id)
    {
        var message = await _context.Messages.FindAsync(id);
        if (message != null)
        {
            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult CreateModerator()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> CreateModerator(CreateModeratorViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Asignăm rolul de Moderator
                await _userManager.AddToRoleAsync(user, "Moderator");

                // Creăm profilul utilizatorului
                var profile = new Profile
                {
                    UserId = user.Id,
                    Name = model.Email.Split('@')[0],
                    Visibility = "Public"
                };

                _context.Profiles.Add(profile);
                await _context.SaveChangesAsync();

                // Actualizăm ProfileId-ul utilizatorului
                user.ProfileId = profile.Id;
                await _userManager.UpdateAsync(user);

                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> FixUserRole(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return NotFound("Utilizatorul nu a fost găsit.");
        }

        // Ștergem toate rolurile existente
        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);

        // Adăugăm rolul de User
        await _userManager.AddToRoleAsync(user, "User");

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ResetUserAccount(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return NotFound("Utilizatorul nu a fost găsit.");
        }

        // Resetăm parola la o valoare cunoscută
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, "Parola123!");

        if (result.Succeeded)
        {
            // Ne asigurăm că email-ul este confirmat
            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);

            // Ștergem toate rolurile existente
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            // Adăugăm rolul de User
            await _userManager.AddToRoleAsync(user, "User");

            return Content($"Contul a fost resetat. Email: {email}, Parolă: Parola123!");
        }

        return Content("A apărut o eroare la resetarea contului.");
    }

    [HttpGet]
    public async Task<IActionResult> FixGoogleUser()
    {
        var email = "google.com";
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return NotFound("Utilizatorul nu a fost găsit.");
        }

        // Ștergem toate rolurile existente
        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);

        // Adăugăm rolul de User
        await _userManager.AddToRoleAsync(user, "User");

        // Ne asigurăm că email-ul este confirmat
        user.EmailConfirmed = true;
        await _userManager.UpdateAsync(user);

        return Content($"Contul pentru {email} a fost reparat. Rolul a fost setat la User.");
    }
}
