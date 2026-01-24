using ChatAppProj.RepositoryContracts;
using Microsoft.EntityFrameworkCore;
using ChatAppProj.Models;
namespace ChatAppProj.Repositories;

public class ConversationRepository 
    : GenericRepository<Conversation>, IConversationRepository
{
    private readonly AppDbContext _context;

    public ConversationRepository(AppDbContext context): base(context)
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
}