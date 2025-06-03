using Microsoft.Extensions.Hosting;
using System.Text.RegularExpressions;
using System.ComponentModel.DataAnnotations;

public class Profile
{
    public int Id { get; set; }
    
    [Required]
    public string UserId { get; set; }
    
    [Required]
    public string Name { get; set; }
    
    public string FirstName { get; set; }
    
    public string LastName { get; set; }
    
    public string Bio { get; set; }
    
    public string ProfilePicture { get; set; }
    
    public string Visibility { get; set; } = "Public";
    
    public virtual ICollection<Post> Posts { get; set; }
    
    public virtual ICollection<Album> Albums { get; set; }
    
    public virtual ApplicationUser User { get; set; }
}
