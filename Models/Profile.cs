using Microsoft.Extensions.Hosting;
using System.Text.RegularExpressions;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Profile
{
    public int Id { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [ForeignKey("UserId")]
    public virtual ApplicationUser? User { get; set; }
    
    public string? Name { get; set; }
    
    public string? FirstName { get; set; }
    
    public string? LastName { get; set; }
    
    public string? Bio { get; set; }
    
    public string? ProfilePicture { get; set; }
    
    public string Visibility { get; set; } = "Public";
    
    public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
    
    public virtual ICollection<Album> Albums { get; set; } = new List<Album>();
    
    public int? GroupId { get; set; }
    
    [ForeignKey("GroupId")]
    public virtual Group? Group { get; set; }
}
