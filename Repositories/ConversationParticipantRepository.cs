using ChatAppProj.RepositoryContracts;
using ChatAppProj.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatAppProj.Repositories;
public class ConversationParticipantRepository
    : GenericRepository<ConversationParticipant>,
      IConversationParticipantRepository
{
    private readonly AppDbContext _context;

    public ConversationParticipantRepository(AppDbContext context): base(context)
    {
        _context = context;
    }

    public bool IsUserInConversation(int conversationId, int userId)
    {
        return _context.ConversationParticipants
            .Any(p => p.ConversationId == conversationId && p.UserId == userId);
    }

    public bool IsAdminOrCreator(int conversationId, int userId)
    {
        return _context.ConversationParticipants
            .Any(p => p.ConversationId == conversationId
                   && p.UserId == userId
                   && (p.Role == ConversationRole.Admin ||
                       p.Role == ConversationRole.Creator));
    }

    public ConversationParticipant? GetParticipant(int conversationId, int userId)
    {
        return _context.ConversationParticipants
            .FirstOrDefault(p => p.ConversationId == conversationId && p.UserId == userId);
    }
}
