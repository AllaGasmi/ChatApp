using ChatAppProj.Models;
using ChatAppProj.RepositoryContracts;
using Microsoft.EntityFrameworkCore;

namespace ChatAppProj.Repositories;

public class ConversationRequestRepository : GenericRepository<ConversationRequest>, IConversationRequestRepository
{
    private readonly AppDbContext _context;

    public ConversationRequestRepository(AppDbContext context) : base(context)
    {
        _context = context;
    }

    public List<ConversationRequest> GetPendingRequestsForUser(int userId)
    {
        return _context.ConversationRequests
            .Include(cr => cr.Requester)
            .Include(cr => cr.Receiver)
            .Where(cr => cr.ReceiverId == userId && cr.Status == ConversationRequestStatus.Pending)
            .OrderByDescending(cr => cr.RequestedAt)
            .ToList();
    }

    public List<ConversationRequest> GetSentRequestsByUser(int userId)
    {
        return _context.ConversationRequests
            .Include(cr => cr.Requester)
            .Include(cr => cr.Receiver)
            .Where(cr => cr.RequesterId == userId && cr.Status == ConversationRequestStatus.Pending)
            .OrderByDescending(cr => cr.RequestedAt)
            .ToList();
    }

    public ConversationRequest? GetRequest(int requesterId, int receiverId, ConversationType type)
    {
        return _context.ConversationRequests
            .Include(cr => cr.Requester)
            .Include(cr => cr.Receiver)
            .FirstOrDefault(cr => cr.RequesterId == requesterId 
                               && cr.ReceiverId == receiverId 
                               && cr.ConversationType == type
                               && cr.Status == ConversationRequestStatus.Pending);
    }
    public ConversationRequest? GetRequest(int requesterId, int receiverId, ConversationType type, string? groupName = null)
    {
        var query = _context.ConversationRequests
            .Include(cr => cr.Requester)
            .Include(cr => cr.Receiver)
            .Where(cr => cr.RequesterId == requesterId 
                    && cr.ReceiverId == receiverId 
                    && cr.ConversationType == type
                    && cr.Status == ConversationRequestStatus.Pending);
        
        if (type == ConversationType.Group && !string.IsNullOrEmpty(groupName))
        {
            query = query.Where(cr => cr.GroupName == groupName);
        }
        
        return query.FirstOrDefault();
    }
}