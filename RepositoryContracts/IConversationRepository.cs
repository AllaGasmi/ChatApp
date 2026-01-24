namespace ChatAppProj.RepositoryContracts;
using ChatAppProj.Models;

public interface IConversationRepository : IGenericRepository<Conversation>
{
    List<Conversation> GetUserConversations(int userId);
    Conversation? GetConversationWithDetails(int conversationId);
    int GetActiveConversationsCount(int userId);
}
