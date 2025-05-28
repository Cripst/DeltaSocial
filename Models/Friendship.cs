using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Friendship
{
    public int Id { get; set; }
    
    [Required]
    public string SenderId { get; set; }
    
    [Required]
    public string ReceiverId { get; set; }
    
    [Required]
    public string Status { get; set; } // "Pending", "Accepted", "Rejected"
    
    [ForeignKey("SenderId")]
    [InverseProperty("SentFriendships")]
    public virtual ApplicationUser Sender { get; set; }
    
    [ForeignKey("ReceiverId")]
    [InverseProperty("ReceivedFriendships")]
    public virtual ApplicationUser Receiver { get; set; }
}
