namespace ChatAppProj.RepositoryContracts;
using ChatAppProj.Models;

public interface IMessageRepository : IGenericRepository<Message>
{
    List<Message> GetConversationMessages(int conversationId);
}