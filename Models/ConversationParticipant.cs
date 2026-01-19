public class ConversationParticipant
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public Conversation Conversation { get; set; }
    public int UserId { get; set; }
    public ApplicationUser User { get; set; }
    public DateTime JoinedAt { get; set; }
}
