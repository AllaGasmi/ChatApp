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
}
