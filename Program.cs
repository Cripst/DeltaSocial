using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using DeltaSocial.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurează DbContext-ul și Identity cu roluri
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()  // Adăugăm suport pentru roluri
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Add EmailSender service
builder.Services.AddTransient<IEmailSender<ApplicationUser>, EmailSender>();

// *** 2. Configurează CORS aici ***
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

// Add JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});

// 3. Suport pentru Razor Pages și MVC
builder.Services.AddRazorPages();
builder.Services.AddControllers(options =>
{
    options.EnableEndpointRouting = true;
}).AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
});
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient(); // Adăugăm suport pentru HttpClient

// Configurare HTTPS
builder.Services.AddHttpsRedirection(options =>
{
    options.HttpsPort = 7236;
    options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
});

// Configurare Kestrel pentru HTTPS
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(7236, listenOptions =>
    {
        listenOptions.UseHttps();
    });
});

var app = builder.Build();

// 4. Middleware-uri
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// *** 5. Folosește CORS middleware-ul chiar înainte de Authentication și Authorization ***
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

// 6. Mapping pentru Razor Pages și controller routes
app.MapRazorPages();
app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// 7. Creare roluri și cont moderator la pornirea aplicației
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // Creare roluri
    var roles = new[] { "Visitor", "User", "Moderator" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    // Creare cont moderator implicit
    var moderatorEmail = "moderator@deltasocial.com";
    var moderatorUser = await userManager.FindByEmailAsync(moderatorEmail);

    if (moderatorUser == null)
    {
        moderatorUser = new ApplicationUser
        {
            UserName = moderatorEmail,
            Email = moderatorEmail,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(moderatorUser, "Moderator123!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(moderatorUser, "Moderator");

            // Creăm profilul pentru moderator
            var profile = new Profile
            {
                UserId = moderatorUser.Id,
                Name = "Moderator",
                Visibility = "Public"
            };

            context.Profiles.Add(profile);
            await context.SaveChangesAsync();

            // Actualizăm ProfileId-ul utilizatorului
            moderatorUser.ProfileId = profile.Id;
            await userManager.UpdateAsync(moderatorUser);
        }
    }

    // Asignăm rolul de User tuturor utilizatorilor existenți care nu au rol
    var users = await userManager.Users.ToListAsync();
    foreach (var user in users)
    {
        var userRoles = await userManager.GetRolesAsync(user);
        if (!userRoles.Any())
        {
            await userManager.AddToRoleAsync(user, "User");
        }
    }
}

app.Run();
