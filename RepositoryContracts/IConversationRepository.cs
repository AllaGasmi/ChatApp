namespace ChatAppProj.RepositoryContracts;
using ChatAppProj.Models;

public interface IConversationRepository : IGenericRepository<Conversation>
{
    List<Conversation> GetUserConversations(int userId);
    Conversation? GetConversationWithDetails(int conversationId);
    int GetActiveConversationsCount(int userId);
    List<Conversation> GetAllWithParticipants();
    public Conversation? GetPrivateConversation(int user1Id, int user2Id);
}
