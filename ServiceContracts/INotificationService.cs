using ChatAppProj.Models;

namespace ChatAppProj.ServiceContracts;

public interface INotificationService {
    bool SendNotification(int userId, Notification notification);
    void RemoveSeen(int userId);
    void MakeSeen(int userId);
}