using ChatAppProj.Models;
using ChatAppProj.Repositories;
using ChatAppProj.RepositoryContracts;
using ChatAppProj.ServiceContracts;

namespace ChatAppProj.Services;

public class NotificationService : INotificationService {
    private readonly IUserRepository _userRepository;

    public NotificationService(IUserRepository userRepository) {
        _userRepository = userRepository;
    }
    
    public bool SendNotification(int userId, Notification notification) {
        var user = _userRepository.GetById(userId);

        if (user == null) {
            return false;
        }
        
        user.Notifications.Add(notification);
        _userRepository.Update(user);

        return true;
    }

    public void RemoveSeen(int userId) {
        _userRepository.RemoveSeen(userId);
    }

    public void MakeSeen(int userId) {
        _userRepository.MakeAllSeen(userId);
    }
}