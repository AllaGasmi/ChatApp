using ChatAppProj.Models;

namespace ChatAppProj.RepositoryContracts;

public interface IConversationRequestRepository : IGenericRepository<ConversationRequest>
{
    List<ConversationRequest> GetPendingRequestsForUser(int userId);
    List<ConversationRequest> GetSentRequestsByUser(int userId);
    ConversationRequest? GetRequest(int requesterId, int receiverId, ConversationType type);
    ConversationRequest? GetRequest(int requesterId, int receiverId, ConversationType type, string? groupName = null);
}