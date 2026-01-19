public class Conversation
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public ConversationType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<ConversationParticipant> Participants { get; set; }
    public ICollection<Message> Messages { get; set; }
}
public enum ConversationType { Private, Group }