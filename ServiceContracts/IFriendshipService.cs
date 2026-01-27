namespace ChatAppProj.ServiceContracts;

public interface IFriendshipService {
    bool AreFriends(int user1Id, int user2Id);
    public bool HasBlocked(int user1Id, int user2Id);
    public bool HasBeenBlockedBy(int user1Id, int user2Id);
    public bool HasRequested(int user1Id, int user2Id);
    SendFriendRequestResponse SendFriendRequest(int senderId, int receiverId);
    void AcceptRequest(int requestId);
    void DeclineRequest(int requestId);
    void DeleteDeclined(int userId);
    BlockUserResponse BlockUser(int blockerId, int blockedId);
    bool UnblockUser(int blockerId, int blockedId);
    bool UnfriendUser(int senderId, int receiverId);
    void CancelRequest(int requestId);
}

public enum SendFriendRequestResponse { Ok, AlreadyFriends, AlreadySent, Blocked, Declined, RequestsDisabled }

public enum BlockUserResponse { Ok, AlreadyBlocked, AlreadyFriends }