using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Message
{
    public int Id { get; set; }
    
    [Required]
    public string SenderId { get; set; }
    
    [Required]
    public string ReceiverId { get; set; }
    
    [Required]
    public string Content { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsRead { get; set; } = false;
    
    [ForeignKey("SenderId")]
    [InverseProperty("SentMessages")]
    public virtual ApplicationUser Sender { get; set; }
    
    [ForeignKey("ReceiverId")]
    [InverseProperty("ReceivedMessages")]
    public virtual ApplicationUser Receiver { get; set; }
}
