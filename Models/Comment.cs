using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Comment
{
    public int Id { get; set; }
    
    [Required]
    public string Content { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public string UserId { get; set; } = string.Empty;
    
    [ForeignKey("UserId")]
    public virtual ApplicationUser? User { get; set; }
    
    public int? PostId { get; set; }
    
    [ForeignKey("PostId")]
    public virtual Post? Post { get; set; }

    public string ApprovalStatus { get; set; } = "Pending"; // Pending, Accepted, Rejected
}
