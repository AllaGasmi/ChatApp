namespace ChatAppProj.ServiceContracts;
using ChatAppProj.Models;

public interface IConversationService
{
    Conversation CreatePrivateConversation(int user1Id, int user2Id);
    Conversation CreateGroupConversation(string name, int creatorId, List<int> userIds);
     List<Conversation> GetUserConversations(int userId);
    List<MessageDto> SendMessage(int conversationId, int senderId, string content);
    void AddParticipant(int conversationId, int actorId, int targetUserId);
    void RemoveParticipant(int conversationId, int actorId, int targetUserId);
    void AssignAdmin(int conversationId, int actorId, int targetUserId);
    bool IsUserInConversation(int conversationId, int userId);
    DashboardStatsDto GetDashboardStats(int userId);
    List<RecentConversationDto> GetRecentConversations(int userId, int count = 3);
    void UpdateGroupInfo(int conversationId, int actorId, string? groupName, IFormFile? groupPicture);
    void LeaveGroup(int conversationId, int userId);
    bool IsAdminOrCreator(int conversationId, int userId);
    void EnsureAiConversation(int userId);


}