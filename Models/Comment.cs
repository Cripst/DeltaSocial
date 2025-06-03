using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Comment
{
    public int Id { get; set; }
    
    [Required]
    public string Content { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public string UserId { get; set; }
    
    [ForeignKey("UserId")]
    public virtual ApplicationUser User { get; set; }
    
    public int PostId { get; set; }
    
    [ForeignKey("PostId")]
    public virtual Post Post { get; set; }
}
