using ChatAppProj.RepositoryContracts;
using ChatAppProj.ServiceContracts;
using ChatAppProj.Models;

public class ConversationService : IConversationService
{
    private readonly IConversationRepository _conversationRepo;
    private readonly IMessageRepository _messageRepo;
    private readonly IConversationParticipantRepository _participantRepo;

    public ConversationService(IConversationRepository conversationRepo,IMessageRepository messageRepo,IConversationParticipantRepository participantRepo)
    {
        _conversationRepo = conversationRepo;
        _messageRepo = messageRepo;
        _participantRepo = participantRepo;
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

    public MessageDto  SendMessage(int conversationId, int senderId, string content)
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


}
