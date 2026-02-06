using ChatAppProj.Models;
using ChatAppProj.RepositoryContracts;
using System.Text.Json;

namespace ChatAppProj.ServiceContracts;

public interface IConversationRequestService
{
    ConversationRequest SendConversationRequest(int requesterId, int receiverId, ConversationType type, string? groupName = null, List<int>? additionalUserIds = null, string? message = null);
    void AcceptRequest(int requestId, int userId);
    void DeclineRequest(int requestId, int userId);
    void CancelRequest(int requestId, int userId);
    List<ConversationRequest> GetPendingRequests(int userId);
    List<ConversationRequest> GetSentRequests(int userId);
    bool HasPendingRequest(int requesterId, int receiverId, ConversationType type);
    public ConversationRequest? GetRequestById(int requestId);
    
}