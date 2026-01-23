using ChatAppProj.RepositoryContracts;
using ChatAppProj.Models;

namespace ChatAppProj.Repositories;

public class FriendshipRepository : GenericRepository<Friendship>, IFriendshipRepository {
    private readonly AppDbContext _context;

    public FriendshipRepository(AppDbContext context) : base(context) {
        _context = context;
    }

    public Friendship? GetFriendship(int user1, int user2) {
        return _context.Friendships
            .FirstOrDefault(f => (f.RequesterId == user1 && f.AddresseeId == user2)
                                      ||  (f.RequesterId == user2 && f.AddresseeId == user1));
    }

    public List<Friendship> GetAllFriendships(int user) {
        return _context.Friendships
            .Where(f => (f.RequesterId == user) || (f.AddresseeId == user))
            .ToList();
    }

    public List<ApplicationUser> GetAllFriends(int user) {
        var friendships = GetAllFriendships(user).Where(f => f.Status == FriendshipStatus.Accepted);
        
        return friendships
            .Select(f => f.RequesterId != user ? f.Requester : f.Addressee)
            .Distinct()
            .ToList();
    }

    public List<ApplicationUser> GetAllBlocked(int user) {
        var friendships = GetAllFriendships(user).Where(f => f.Status == FriendshipStatus.Blocked);
        
        return friendships
            .Select(f => f.RequesterId != user ? f.Requester : f.Addressee)
            .Distinct()
            .ToList();
    }

    public List<Friendship> GetAllPending(int user) {
        return GetAllFriendships(user)
            .Where(f => f.Status == FriendshipStatus.Pending)
            .ToList();
    }
}