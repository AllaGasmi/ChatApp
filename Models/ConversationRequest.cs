using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace ChatAppProj.Models;

public class ConversationRequest
{
    public int Id { get; set; }
    public int RequesterId { get; set; }
    public ApplicationUser Requester { get; set; } = null!;
    public int ReceiverId { get; set; }
    public ApplicationUser Receiver { get; set; } = null!;
    public ConversationType ConversationType { get; set; }
    public string? GroupName { get; set; }
    
    public string? AdditionalUserIdsJson { get; set; }
    
    [NotMapped]
    public List<int>? AdditionalUserIds
    {
        get
        {
            if (string.IsNullOrWhiteSpace(AdditionalUserIdsJson))
                return null;

            try
            {
                return JsonSerializer.Deserialize<List<int>>(AdditionalUserIdsJson);
            }
            catch
            {
                return null;
            }
        }
        set
        {
            AdditionalUserIdsJson = value == null || value.Count == 0
                ? null
                : JsonSerializer.Serialize(value);
        }
    }
    
    public ConversationRequestStatus Status { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public string? Message { get; set; }
}

public enum ConversationRequestStatus{Pending,Accepted,Declined}