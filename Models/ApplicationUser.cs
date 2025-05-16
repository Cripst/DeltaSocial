using Microsoft.AspNetCore.Identity;
using static System.Runtime.InteropServices.JavaScript.JSType;

public class ApplicationUser : IdentityUser
{
    public Profile Profile { get; set; }
}
