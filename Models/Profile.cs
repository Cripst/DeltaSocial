using Microsoft.Extensions.Hosting;
using System.Text.RegularExpressions;

public class Profile
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public string Name { get; set; }
    public string Visibility { get; set; } // "Public" / "Private"
    public ICollection<Post> Posts { get; set; }
    public ICollection<Album> Albums { get; set; }
    public ICollection<Group> Groups { get; set; }
}
