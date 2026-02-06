using ChatAppProj.Models;

namespace ChatAppProj.RepositoryContracts;

public interface IUserRepository : IGenericRepository<ApplicationUser> {
    List<ApplicationUser> GetUsersWhoAllowPrivateChats(int userId);
    List<ApplicationUser> GetUsersWhoAllowRequests();
    List<ApplicationUser> GetUsersWhoAllowGroupChats();
    UserConfiguration GetUserConfiguration(int userId);
    List<ApplicationUser> SearchUsers(string search);
    void UpdateUserConfiguration(UserConfiguration userConfiguration);
    void setOnline(int userId);
    void setOffline(int userId);
    List<Notification> GetNotifications(int userId);
    void RemoveSeen(int userId);
    void MakeAllSeen(int userId);
    bool HasUnseen(int userId);
}