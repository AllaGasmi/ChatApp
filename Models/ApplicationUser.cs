using Microsoft.AspNetCore.Identity;

public class ApplicationUser : IdentityUser<int>
{
    public string? DisplayName { get; set; }
    public string? ProfilePicture { get; set; }
    public bool IsOnline { get; set; }
    public DateTime? LastSeen { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public ICollection<Friendship> SentFriendRequests { get; set; }
    public ICollection<Friendship> ReceivedFriendRequests { get; set; }
    public ICollection<Message> Messages { get; set; }
}
