using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Post
{
    public int Id { get; set; }
    
    [Required]
    public string Title { get; set; }
    
    [Required]
    public string Content { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public int ProfileId { get; set; }
    
    [ForeignKey("ProfileId")]
    public virtual Profile Profile { get; set; }
}
