namespace ChatAppProj.RepositoryContracts;
using ChatAppProj.Models;

public interface IConversationParticipantRepository: IGenericRepository<ConversationParticipant>
{
    bool IsUserInConversation(int conversationId, int userId);
    bool IsAdminOrCreator(int conversationId, int userId);
    ConversationParticipant? GetParticipant(int conversationId, int userId);
}