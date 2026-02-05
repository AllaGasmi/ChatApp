public class MessageDto
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public int? SenderId { get; set; }
    public string? SenderName { get; set; } 
    public string Content { get; set; }
    public bool IsAI { get; set; }
    public DateTime SentAt { get; set; }
}
