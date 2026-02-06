namespace ChatAppProj.Models;
public class Message
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public Conversation Conversation { get; set; }
    public int? SenderId { get; set; }
    public ApplicationUser? Sender { get; set; }
    public string Content { get; set; }
    public bool IsAI { get; set; }
    public DateTime SentAt { get; set; }
    public bool IsRead { get; set; } = false;
}
