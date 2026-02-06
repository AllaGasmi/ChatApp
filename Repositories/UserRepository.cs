using ChatAppProj.Models;
using ChatAppProj.RepositoryContracts;
using ChatAppProj.ServiceContracts;
using Microsoft.EntityFrameworkCore;

namespace ChatAppProj.Repositories;

public class UserRepository : GenericRepository<ApplicationUser>, IUserRepository {
    private readonly AppDbContext _context;
    private readonly IFriendshipService _friendshipService;
    
    public UserRepository(AppDbContext context, IFriendshipService friendshipService) : base(context) {
        _context = context;
        _friendshipService = friendshipService;
    }

    public new ApplicationUser? GetById(int userId) {
        return _context.Users
            .Include(u => u.Notifications) // This loads the Notifications
            .FirstOrDefault(u => u.Id == userId);
    }
    
    public List<ApplicationUser> GetUsersWhoAllowPrivateChats(int userId) {
        var users = _context.Users
            .Include(u => u.UserConfiguration)
            .Where(u => u.UserConfiguration.AllowRequest)
            .AsEnumerable()
            .ToList();
    
        var usersWithChatRestriction = users
            .Where(u => !u.UserConfiguration.AllowOnlyFriendsChat || 
                        _friendshipService.AreFriends(userId, u.Id))
            .ToList();
    
        return usersWithChatRestriction;
    }

    public List<ApplicationUser> GetUsersWhoAllowRequests() {
        return _context.Users
            .Include(u => u.UserConfiguration)
            .Where(u => u.UserConfiguration.AllowRequest)
            .ToList();
    }

    public List<ApplicationUser> GetUsersWhoAllowGroupChats() {
        return _context.Users
            .Include(u => u.UserConfiguration)
            .Where(u => u.UserConfiguration.AllowBeingAddedToGroup)
            .ToList();
    }

    public UserConfiguration GetUserConfiguration(int userId) {
        return _context.Users
            .Where(u => u.Id == userId)
            .Include(u => u.UserConfiguration)
            .Select(u => u.UserConfiguration).First();
    }

    public List<ApplicationUser> SearchUsers(string search) {
        var searchTerm = search.Trim().ToLower();
        
        return _context.Users
            .Where(u => u.DisplayName.Trim().ToLower().Contains(searchTerm))
            .Include(u => u.UserConfiguration)
            .ToList();
    }

    public void UpdateUserConfiguration(UserConfiguration userConfiguration) {
        _context.Attach(userConfiguration);
        _context.Entry(userConfiguration).State = EntityState.Modified;
        _context.SaveChanges();
    }

    public void setOnline(int userId) {
        var user = _context.Users.Single(u => u.Id == userId);

        user.IsOnline = true;
        _context.SaveChanges();
    }

    public void setOffline(int userId) {
        var user = _context.Users.Single(u => u.Id == userId);

        user.IsOnline = false;
        user.LastSeen = DateTime.Now;
        _context.SaveChanges();
    }

    public List<Notification> GetNotifications(int userId) {
        return _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.SentAt)
            .ToList();
    }

    public void RemoveSeen(int userId) {
        var notifications = _context.Notifications
            .Where(n => n.UserId == userId && n.IsSeen)
            .ToList();
    
        _context.Notifications.RemoveRange(notifications);
        _context.SaveChanges();
    }

    public void MakeAllSeen(int userId) {
        var notifications = _context.Notifications
            .Where(n => n.UserId == userId && !n.IsSeen)
            .ToList();
    
        foreach (var notification in notifications) {
            notification.IsSeen = true;
        }
    
        _context.SaveChanges();
    }

    public bool HasUnseen(int userId) {
        return _context.Notifications
            .Any(n => n.UserId == userId && !n.IsSeen);
    }
}