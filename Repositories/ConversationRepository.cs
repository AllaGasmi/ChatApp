using ChatAppProj.RepositoryContracts;
using ChatAppProj.Models;
namespace ChatAppProj.Repositories;
using Microsoft.EntityFrameworkCore;

public class ConversationRepository
    : GenericRepository<Conversation>, IConversationRepository
{
    private readonly AppDbContext _context;

    public ConversationRepository(AppDbContext context) : base(context)
    {
        _context = context;
    }

    public List<Conversation> GetUserConversations(int userId)
    {
        return _context.Conversations
            .Include(c => c.Participants)
                .ThenInclude(p => p.User)
            .Include(c => c.Messages.OrderByDescending(m => m.SentAt).Take(1))
            .Where(c => c.Participants.Any(p => p.UserId == userId))
            .OrderByDescending(c => c.CreatedAt)
            .ToList();
    }

    public Conversation? GetConversationWithDetails(int conversationId)
    {
        return _context.Conversations
            .Include(c => c.Participants)
            .ThenInclude(p => p.User)
            .Include(c => c.Messages)
            .ThenInclude(m => m.Sender)
            .FirstOrDefault(c => c.Id == conversationId);
    }
    public int GetActiveConversationsCount(int userId)
    {
        var weekAgo = DateTime.UtcNow.AddDays(-7);

        return _context.Conversations
            .Where(c => c.Participants.Any(p => p.UserId == userId)
                     && c.Messages.Any(m => m.SentAt >= weekAgo))
            .Count();
    }

    public List<Conversation> GetAllWithParticipants()
    {
        return _context.Conversations
            .Include(c => c.Participants)
            .ThenInclude(p => p.User)
            .ToList();
    }
    public Conversation? GetPrivateConversation(int user1Id, int user2Id)
    {
        return _context.Conversations
            .Include(c => c.Participants)
            .ThenInclude(p => p.User)
            .Where(c => c.Type == ConversationType.Private)
            .AsEnumerable() 
            .FirstOrDefault(c => 
                c.Participants.Count == 2 &&
                c.Participants.Any(p => p.UserId == user1Id) &&
                c.Participants.Any(p => p.UserId == user2Id));
    }

    
}