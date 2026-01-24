using ChatAppProj.RepositoryContracts;
using ChatAppProj.ServiceContracts;
using ChatAppProj.Models;
using Microsoft.AspNetCore.Identity;

public class ConversationService : IConversationService
{
    private readonly IConversationRepository _conversationRepo;
    private readonly IMessageRepository _messageRepo;
    private readonly IConversationParticipantRepository _participantRepo;
    private readonly UserManager<ApplicationUser> _userManager;

    public ConversationService(IConversationRepository conversationRepo,IMessageRepository messageRepo,IConversationParticipantRepository participantRepo,UserManager<ApplicationUser> userManager)
    {
        _conversationRepo = conversationRepo;
        _messageRepo = messageRepo;
        _participantRepo = participantRepo;
        _userManager = userManager;
    }

    public Conversation CreatePrivateConversation(int creatorId, int otherUserId)
    {
        var conversation = new Conversation
        {
            Type = ConversationType.Private,
            CreatedAt = DateTime.UtcNow,
            Participants = new List<ConversationParticipant>
            {
                new()
                {
                    UserId = creatorId,
                    Role = ConversationRole.Creator,
                    JoinedAt = DateTime.UtcNow
                },
                new()
                {
                    UserId = otherUserId,
                    Role = ConversationRole.Member,
                    JoinedAt = DateTime.UtcNow
                }
            }
        };

        _conversationRepo.Create(conversation);
        return conversation;
    }

    public Conversation CreateGroupConversation(string name, int creatorId, List<int> userIds)
    {
        var participants = userIds.Select(id => new ConversationParticipant
        {
            UserId = id,
            Role = ConversationRole.Member,
            JoinedAt = DateTime.UtcNow
        }).ToList();

        participants.Add(new ConversationParticipant
        {
            UserId = creatorId,
            Role = ConversationRole.Creator,
            JoinedAt = DateTime.UtcNow
        });

        var conversation = new Conversation
        {
            Name = name,
            Type = ConversationType.Group,
            CreatedAt = DateTime.UtcNow,
            Participants = participants
        };

        _conversationRepo.Create(conversation);
        return conversation;
    }

    public List<Conversation> GetUserConversations(int userId)
    {
        return _conversationRepo.GetUserConversations(userId);
    }

    public MessageDto SendMessage(int conversationId, int senderId, string content)
    {
        if (!_participantRepo.IsUserInConversation(conversationId, senderId))
            throw new UnauthorizedAccessException("User not in conversation");

        var message = new Message
        {
            ConversationId = conversationId,
            SenderId = senderId,
            Content = content,
            IsAI = false,
            SentAt = DateTime.UtcNow
        };

        _messageRepo.Create(message);
        return new MessageDto
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            SenderId = message.SenderId,
            Content = message.Content,
            IsAI = message.IsAI,
            SentAt = message.SentAt
        };
    }

    public void AddParticipant(int conversationId, int actorId, int targetUserId)
    {
        if (!_participantRepo.IsAdminOrCreator(conversationId, actorId))
            throw new UnauthorizedAccessException("Only admin or creator can add users.");

        if (_participantRepo.IsUserInConversation(conversationId, targetUserId))
            throw new InvalidOperationException("User already in conversation.");

        var participant = new ConversationParticipant
        {
            ConversationId = conversationId,
            UserId = targetUserId,
            Role = ConversationRole.Member,
            JoinedAt = DateTime.UtcNow
        };

        _participantRepo.Create(participant);
    }

    public void RemoveParticipant(int conversationId, int actorId, int targetUserId)
    {
        if (!_participantRepo.IsAdminOrCreator(conversationId, actorId))
            throw new UnauthorizedAccessException("Only admin or creator can remove users.");

        var participant = _participantRepo.GetParticipant(conversationId, targetUserId)
            ?? throw new InvalidOperationException("Participant not found.");

        if (participant.Role == ConversationRole.Creator)
            throw new InvalidOperationException("Creator cannot be removed.");

        _participantRepo.Delete(participant);
    }

    public void AssignAdmin(int conversationId, int actorId, int targetUserId)
    {
        var actor = _participantRepo.GetParticipant(conversationId, actorId)
            ?? throw new UnauthorizedAccessException();

        if (actor.Role != ConversationRole.Creator)
            throw new UnauthorizedAccessException("Only creator can assign admins.");

        var target = _participantRepo.GetParticipant(conversationId, targetUserId)
            ?? throw new InvalidOperationException("User not in conversation.");

        target.Role = ConversationRole.Admin;
        _participantRepo.Update(target);
    }

    public bool IsUserInConversation(int conversationId, int userId)
    {
        return _participantRepo.IsUserInConversation(conversationId, userId);
    }

    public DashboardStatsDto GetDashboardStats(int userId)
    {
        var unreadCount = _messageRepo.GetUnreadMessageCount(userId);
        var activeChatsCount = _conversationRepo.GetActiveConversationsCount(userId);
        
        var conversations = _conversationRepo.GetUserConversations(userId);
        var privateChatUserIds = conversations
            .Where(c => c.Type == ConversationType.Private)
            .SelectMany(c => c.Participants)
            .Where(p => p.UserId != userId)
            .Select(p => p.UserId)
            .Distinct()
            .ToList();

        var onlineFriendsCount = _userManager.Users
            .Count(u => privateChatUserIds.Contains(u.Id) && u.IsOnline);

        var oneDayAgo = DateTime.UtcNow.AddDays(-1);
        var recentMessages = _messageRepo.GetRecentMessagesForUser(userId, 100);
        var newMessagesCount = recentMessages
            .Count(m => m.SentAt >= oneDayAgo && m.SenderId != userId);

        return new DashboardStatsDto
        {
            NewMessages = newMessagesCount,
            ActiveChats = activeChatsCount,
            UnreadMessages = unreadCount,
            FriendsOnline = onlineFriendsCount
        };
    }

    public List<RecentConversationDto> GetRecentConversations(int userId, int count = 3)
    {
        var conversations = _conversationRepo.GetUserConversations(userId);
        
        return conversations
            .OrderByDescending(c => c.Messages?.Max(m => (DateTime?)m.SentAt) ?? c.CreatedAt)
            .Take(count)
            .Select(c => {
                var lastMessage = c.Messages?.OrderByDescending(m => m.SentAt).FirstOrDefault();
                
                string name;
                string avatarUrl = "https://via.placeholder.com/50";
                bool isOnline = false;

                if (c.Type == ConversationType.Private)
                {
                    var otherParticipant = c.Participants.FirstOrDefault(p => p.UserId != userId);
                    name = otherParticipant?.User?.UserName ?? "Unknown";
                    isOnline = otherParticipant?.User?.IsOnline ?? false;
                }
                else
                {
                    name = c.Name ?? "Group Chat";
                }

                var timeAgo = lastMessage != null 
                    ? GetTimeAgo(lastMessage.SentAt)
                    : "No messages";

                var unreadCount = c.Messages?
                    .Count(m => m.SenderId != userId 
                             && m.SentAt > DateTime.UtcNow.AddDays(-1)) ?? 0;

                return new RecentConversationDto
                {
                    ConversationId = c.Id,
                    Name = name,
                    AvatarUrl = avatarUrl,
                    LastMessage = lastMessage?.Content ?? "No messages yet",
                    TimeAgo = timeAgo,
                    UnreadCount = unreadCount,
                    IsOnline = isOnline
                };
            })
            .ToList();
    }

    private string GetTimeAgo(DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime;

        if (timeSpan.TotalMinutes < 1)
            return "Just now";
        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes}m ago";
        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours}h ago";
        if (timeSpan.TotalDays < 7)
            return $"{(int)timeSpan.TotalDays}d ago";
        
        return dateTime.ToString("MMM dd");
    }
}