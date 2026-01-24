namespace ChatAppProj.RepositoryContracts;
using ChatAppProj.Models;

public interface IMessageRepository : IGenericRepository<Message>
{
    List<Message> GetConversationMessages(int conversationId);
    int GetUnreadMessageCount(int userId);
    int GetTotalMessageCount(int userId);
    List<Message> GetRecentMessagesForUser(int userId, int count = 10);
}