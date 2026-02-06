namespace ChatAppProj.Models;

public class Notification {
    public int Id { get; set; }
    public string Message { get; set; } = "";
    public DateTime SentAt { get; set; } = DateTime.Now;
    public bool IsSeen { get; set; } = false;
    public NotificationType Type { get; set; }
    public int UserId { get; set; }
    public ApplicationUser? User { get; set; }
}

public enum NotificationType { Message, FriendshipAccepted, FriendshipDenied, FriendshipReceived, RequestReceived }