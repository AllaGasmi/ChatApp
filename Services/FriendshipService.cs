using ChatAppProj.RepositoryContracts;
using ChatAppProj.ServiceContracts;
using ChatAppProj.Models;

namespace ChatAppProj.Services;

public class FriendshipService : IFriendshipService {
    private readonly IFriendshipRepository _friendshipRepository;
    private readonly IGenericRepository<ApplicationUser> _userRepository;

    public FriendshipService(IFriendshipRepository friendshipRepository, IGenericRepository<ApplicationUser> userRepository) {
        _friendshipRepository = friendshipRepository;
        _userRepository = userRepository;
    }

    public bool AreFriends(int user1Id, int user2Id) {
        var currentFriendship = _friendshipRepository.GetFriendship(user1Id, user2Id);

        if (currentFriendship == null) {
            return false;
        }

        return currentFriendship.Status == FriendshipStatus.Accepted;
    }

    public bool HasBlocked(int user1Id, int user2Id) {
        var currentFriendship = _friendshipRepository.GetFriendship(user1Id, user2Id);

        if (currentFriendship == null) {
            return false;
        }

        return (currentFriendship.Status == FriendshipStatus.Blocked && currentFriendship.RequesterId == user1Id);
    }

    public bool HasBeenBlockedBy(int user1Id, int user2Id) {
        var currentFriendship = _friendshipRepository.GetFriendship(user1Id, user2Id);

        if (currentFriendship == null) {
            return false;
        }

        return (currentFriendship.Status == FriendshipStatus.Blocked && currentFriendship.AddresseeId == user1Id);
    }

    public bool HasRequested(int user1Id, int user2Id) {
        var currentFriendship = _friendshipRepository.GetFriendship(user1Id, user2Id);

        if (currentFriendship == null) {
            return false;
        }

        return currentFriendship.Status is FriendshipStatus.Pending or FriendshipStatus.Declined;
    }

    public SendFriendRequestResponse SendFriendRequest(int senderId, int receiverId) {
        var sender = _userRepository.GetById(senderId);
        var receiver = _userRepository.GetById(receiverId);

        if (sender == null || receiver == null) {
            throw new ArgumentException();
        }

        if (!receiver.UserConfiguration.AllowRequest) {
            return SendFriendRequestResponse.RequestsDisabled;
        }

        var currentFriendship = _friendshipRepository.GetFriendship(senderId, receiverId);
        if (currentFriendship != null) {
            if (currentFriendship.Status == FriendshipStatus.Accepted) {
                return SendFriendRequestResponse.AlreadyFriends;
            } else if (currentFriendship.Status == FriendshipStatus.Blocked) {
                return SendFriendRequestResponse.Blocked;
            } else if (currentFriendship.Status == FriendshipStatus.Pending) {
                return SendFriendRequestResponse.AlreadySent;
            } else {
                return SendFriendRequestResponse.Declined;
            }
        }

        var request = new Friendship() {
            RequesterId = sender.Id,
            Requester = sender,
            AddresseeId = receiver.Id,
            Addressee = receiver,
            Status = FriendshipStatus.Pending,
            RequestedAt = DateTime.Now
        };
        
        _friendshipRepository.Create(request);
        return SendFriendRequestResponse.Ok;
    }

    public void AcceptRequest(int requestId) {
        var request = _friendshipRepository.GetById(requestId);

        if (request == null) {
            throw new ArgumentException();
        }
        
        request.Status = FriendshipStatus.Accepted;
        request.RespondedAt = DateTime.Now;
        _friendshipRepository.Update(request);
    }

    public void DeclineRequest(int requestId) {
        var request = _friendshipRepository.GetById(requestId);

        if (request == null) {
            throw new ArgumentException();
        }
        
        request.Status = FriendshipStatus.Declined;
        request.RespondedAt = DateTime.Now;
        _friendshipRepository.Update(request);
    }

    public void DeleteDeclined(int userId) {
        var user = _userRepository.GetById(userId);

        if (user == null) {
            throw new ArgumentException();
        }

        var deniedRequests = _friendshipRepository.GetAllFriendships(userId).Where(f => f.Status == FriendshipStatus.Declined);

        foreach (var deniedRequest in deniedRequests) {
            _friendshipRepository.Delete(deniedRequest);
        }
    }

    public BlockUserResponse BlockUser(int blockerId, int blockedId) {
        var blocker = _userRepository.GetById(blockerId);
        var blocked = _userRepository.GetById(blockedId);

        if (blocker == null || blocked == null) {
            throw new ArgumentException();
        }

        if (AreFriends(blockerId, blockedId)) {
            return BlockUserResponse.AlreadyFriends;
        }
        
        var currentFriendship = _friendshipRepository.GetFriendship(blockerId, blockedId);

        if (currentFriendship == null) {
            var request = new Friendship() {
                RequesterId = blocker.Id,
                Requester = blocker,
                AddresseeId = blocked.Id,
                Addressee = blocked,
                Status = FriendshipStatus.Blocked,
                RequestedAt = DateTime.Now,
                RespondedAt = DateTime.Now
            };
        
            _friendshipRepository.Create(request);
        } else if (currentFriendship.Status == FriendshipStatus.Blocked) {
            return BlockUserResponse.AlreadyBlocked;
        } else {
            currentFriendship.Status = FriendshipStatus.Blocked;
            currentFriendship.RespondedAt = DateTime.Now;
            _friendshipRepository.Update(currentFriendship);
        }
        
        return BlockUserResponse.Ok;
    }

    public bool UnblockUser(int blockerId, int blockedId) {
        var blocker = _userRepository.GetById(blockerId);
        var blocked = _userRepository.GetById(blockedId);

        if (blocker == null || blocked == null) {
            throw new ArgumentException();
        }
        
        var currentFriendship = _friendshipRepository.GetFriendship(blockerId, blockedId);

        if (currentFriendship == null) {
            return false;
        }

        if (currentFriendship.Status == FriendshipStatus.Blocked) {
            _friendshipRepository.Delete(currentFriendship);
            return true;
        }
        
        return false;
    }

    public bool UnfriendUser(int senderId, int receiverId) {
        var sender = _userRepository.GetById(receiverId);
        var receiver = _userRepository.GetById(senderId);

        if (sender == null || receiver == null) {
            throw new ArgumentException();
        }
        
        var currentFriendship = _friendshipRepository.GetFriendship(senderId, receiverId);

        if (currentFriendship == null) {
            return false;
        }

        if (currentFriendship.Status == FriendshipStatus.Accepted) {
            _friendshipRepository.Delete(currentFriendship);
            return true;
        }
        
        return false;
    }

    public void CancelRequest(int requestId) {
        var request = _friendshipRepository.GetById(requestId);

        if (request == null) {
            throw new ArgumentException();
        }
        
        _friendshipRepository.Delete(request);
    }
}