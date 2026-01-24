using ChatAppProj.RepositoryContracts;
using ChatAppProj.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatAppProj.Repositories;
public class MessageRepository : GenericRepository<Message>, IMessageRepository
{
    private readonly AppDbContext _context;

    public MessageRepository(AppDbContext context) : base(context)
    {
        _context = context;
    }

    public List<Message> GetConversationMessages(int conversationId)
    {
        return _context.Messages.Where(m => m.ConversationId == conversationId).OrderBy(m => m.SentAt).ToList();
    }
    public int GetUnreadMessageCount(int userId)
    {
        var userConversationIds = _context.ConversationParticipants
            .Where(cp => cp.UserId == userId)
            .Select(cp => cp.ConversationId)
            .ToList();

        return _context.Messages
            .Where(m => userConversationIds.Contains(m.ConversationId) 
                     && m.SenderId != userId
                     && m.SentAt > DateTime.UtcNow.AddDays(-1)) 
            .Count();
    }

    public int GetTotalMessageCount(int userId)
    {
        var userConversationIds = _context.ConversationParticipants
            .Where(cp => cp.UserId == userId)
            .Select(cp => cp.ConversationId)
            .ToList();

        return _context.Messages
            .Where(m => userConversationIds.Contains(m.ConversationId))
            .Count();
    }

    public List<Message> GetRecentMessagesForUser(int userId, int count = 10)
    {
        var userConversationIds = _context.ConversationParticipants
            .Where(cp => cp.UserId == userId)
            .Select(cp => cp.ConversationId)
            .ToList();

        return _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Conversation)
                .ThenInclude(c => c.Participants)
                    .ThenInclude(p => p.User)
            .Where(m => userConversationIds.Contains(m.ConversationId))
            .OrderByDescending(m => m.SentAt)
            .Take(count)
            .ToList();
    }
}
