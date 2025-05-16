using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurează DbContext-ul și Identity cu roluri
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// *** 2. Configurează CORS aici ***
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:3001") // porturile unde rulează React
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// 3. Suport pentru Razor Pages și MVC
builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();

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
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

// 6. Mapping pentru Razor Pages și controller routes
app.MapRazorPages();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers(); // adaugă această linie pentru a activa endpoint-urile API
// 7. Creare roluri la pornirea aplicației
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await CreateRoles(services);
}

app.Run();

// Metodă pentru creare roluri
async Task CreateRoles(IServiceProvider serviceProvider)
{
    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    string[] roleNames = { "Visitor", "User", "Moderator" };

    foreach (var role in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}
