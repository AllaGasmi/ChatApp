using ChatAppProj.RepositoryContracts;
using ChatAppProj.ServiceContracts;
using ChatAppProj.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public class ConversationService : IConversationService
{
    private readonly IConversationRepository _conversationRepo;
    private readonly IMessageRepository _messageRepo;
    private readonly IConversationParticipantRepository _participantRepo;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _environment;
    private readonly IAIService _aiService;

    public ConversationService(IConversationRepository conversationRepo, IMessageRepository messageRepo, IConversationParticipantRepository participantRepo, UserManager<ApplicationUser> userManager, IWebHostEnvironment environment, IAIService aiService)
    {
        _conversationRepo = conversationRepo;
        _messageRepo = messageRepo;
        _participantRepo = participantRepo;
        _userManager = userManager;
        _environment = environment;
        _aiService = aiService;
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

    public List<MessageDto> SendMessage(int conversationId, int senderId, string content)
    {
        
        if (!_participantRepo.IsUserInConversation(conversationId, senderId))
            throw new UnauthorizedAccessException();

        var result = new List<MessageDto>();


        var userMessage = new Message
        {
            ConversationId = conversationId,
            SenderId = senderId,
            Content = content,
            IsAI = false,
            SentAt = DateTime.UtcNow
        };
        _messageRepo.Create(userMessage);

        result.Add(new MessageDto
        {
            Id = userMessage.Id,
            ConversationId = userMessage.ConversationId,
            SenderId = userMessage.SenderId,
            Content = userMessage.Content,
            IsAI = false,
            SentAt = userMessage.SentAt
        });

        var conversation = _conversationRepo.GetConversationWithDetails(conversationId);
        var aiParticipant = conversation.Participants
            .FirstOrDefault(p => p.User.Email == "ai@chatapp.local");

        if (aiParticipant != null && senderId != aiParticipant.UserId)
        {

            string aiReply = _aiService.GetAIResponse(content); 

            var aiMessage = new Message
            {
                ConversationId = conversationId,
                SenderId = aiParticipant.UserId,
                Content = aiReply,
                IsAI = true,
                SentAt = DateTime.UtcNow
            };
            _messageRepo.Create(aiMessage);

            result.Add(new MessageDto
            {
                Id = aiMessage.Id,
                ConversationId = aiMessage.ConversationId,
                SenderId = aiMessage.SenderId,
                Content = aiMessage.Content,
                IsAI = true,
                SentAt = aiMessage.SentAt
            });
        }

        return result;
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
            .Select(c =>
            {
                var lastMessage = c.Messages?.OrderByDescending(m => m.SentAt).FirstOrDefault();

                string name;
                string avatarUrl;
                bool isOnline = false;

                if (c.Type == ConversationType.Private)
                {
                    var otherParticipant = c.Participants.FirstOrDefault(p => p.UserId != userId);
                    name = otherParticipant?.User?.UserName ?? "Unknown";
                    isOnline = otherParticipant?.User?.IsOnline ?? false;
                    avatarUrl = otherParticipant?.User?.ProfilePicture;
                }
                else
                {
                    name = c.Name ?? "Group Chat";
                    avatarUrl = c.GroupPicture;
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
    public bool IsAdminOrCreator(int conversationId, int userId)
    {
        return _participantRepo.IsAdminOrCreator(conversationId, userId);
    }

    public void UpdateGroupInfo(int conversationId, int actorId, string? groupName, IFormFile? groupPicture)
    {
        if (!_participantRepo.IsAdminOrCreator(conversationId, actorId))
            throw new UnauthorizedAccessException("Only admin or creator can update group info.");

        var conversation = _conversationRepo.GetById(conversationId)
            ?? throw new InvalidOperationException("Conversation not found.");

        if (conversation.Type != ConversationType.Group)
            throw new InvalidOperationException("Can only update group conversations.");

        if (!string.IsNullOrWhiteSpace(groupName))
        {
            conversation.Name = groupName;
        }

        if (groupPicture != null && groupPicture.Length > 0)
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "groups");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{conversationId}_{Guid.NewGuid()}{Path.GetExtension(groupPicture.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                groupPicture.CopyTo(fileStream);
            }

            conversation.GroupPicture = $"/uploads/groups/{uniqueFileName}";
        }

        _conversationRepo.Update(conversation);
    }

    public void LeaveGroup(int conversationId, int userId)
    {
        var participant = _participantRepo.GetParticipant(conversationId, userId)
            ?? throw new InvalidOperationException("You are not in this conversation.");

        if (participant.Role == ConversationRole.Creator)
            throw new InvalidOperationException("Creator cannot leave the group. Transfer ownership first.");

        _participantRepo.Delete(participant);
    }

    public void EnsureAiConversation(int userId)
    {
        var aiUser = _userManager.Users.FirstOrDefault(u => u.Email == "ai@chatapp.local");
        if (aiUser == null) return;

        var exists = _conversationRepo.GetAllWithParticipants()
            .Any(c =>
                c.Type == ConversationType.Private && c.Participants != null &&
                c.Participants.Any(p => p.UserId == userId) &&
                c.Participants.Any(p => p.UserId == aiUser.Id));

        if (!exists)
        {
            var conversation = new Conversation
            {
                Name = "AI Chat",
                Type = ConversationType.Private,
                CreatedAt = DateTime.UtcNow,
                Participants = new List<ConversationParticipant>
            {
                new()
                {
                    UserId = userId,
                    Role = ConversationRole.Member,
                    JoinedAt = DateTime.UtcNow
                },
                new()
                {
                    UserId = aiUser.Id,
                    Role = ConversationRole.Member,
                    JoinedAt = DateTime.UtcNow
                }
            }
            };

            _conversationRepo.Create(conversation);
        }
    }
    public Conversation? GetPrivateConversation(int user1Id, int user2Id)
    {
        return _conversationRepo.GetPrivateConversation(user1Id,user2Id);
    }

    // public List<MessageDto> SendMessage(int conversationId, int senderId, string content)
    // {
    //     throw new NotImplementedException();
    // }
}