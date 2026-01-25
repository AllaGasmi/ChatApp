using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
namespace ChatAppProj.Models;
public class ApplicationUser : IdentityUser<int>
{
    public string? DisplayName { get; set; }
    public string? ProfilePicture { get; set; }
    public bool IsOnline { get; set; }
    public DateTime? LastSeen { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(500)] 
    public string? Bio { get; set; }

    public UserConfiguration UserConfiguration { get; set; } = new UserConfiguration()
    {
        AllowBeingAddedToGroup = true,
        AllowOnlyFriendsChat = false,
        AllowRequest = true
    };
    public ICollection<Friendship> SentFriendRequests { get; set; }
    public ICollection<Friendship> ReceivedFriendRequests { get; set; }
    public ICollection<Message> Messages { get; set; }
}
