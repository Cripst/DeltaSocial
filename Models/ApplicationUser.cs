using Microsoft.AspNetCore.Identity;
using static System.Runtime.InteropServices.JavaScript.JSType;

public class ApplicationUser : IdentityUser
{
    public int? ProfileId { get; set; }
    public Profile Profile { get; set; }
    
    public virtual ICollection<Friendship> SentFriendships { get; set; }
    public virtual ICollection<Friendship> ReceivedFriendships { get; set; }
}
