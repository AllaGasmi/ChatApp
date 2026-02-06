namespace ChatAppProj.Models;
public class Friendship
{
    public int Id { get; set; }
    public int RequesterId { get; set; }
    public ApplicationUser Requester { get; set; }
    public int AddresseeId { get; set; }
    public ApplicationUser Addressee { get; set; }
    public FriendshipStatus Status { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
}

public enum FriendshipStatus { Pending, Accepted, Declined, Blocked }
