using ChatAppProj.Models;
using ChatAppProj.RepositoryContracts;
using ChatAppProj.ServiceContracts;
using Microsoft.AspNetCore.SignalR;

namespace ChatAppProj.Services;

public class ConversationRequestService : IConversationRequestService
{
    private readonly IConversationRequestRepository _requestRepository;
    private readonly IConversationService _conversationService;
    private readonly IGenericRepository<ApplicationUser> _userRepository;
    private readonly IHubContext<ChatHub> _hubContext;

    public ConversationRequestService(IConversationRequestRepository requestRepository,IConversationService conversationService,IGenericRepository<ApplicationUser> userRepository,IHubContext<ChatHub> hubContext)
    {
        _requestRepository = requestRepository;
        _conversationService = conversationService;
        _userRepository = userRepository;
        _hubContext = hubContext;
        
    }

    public ConversationRequest? GetRequestById(int requestId)
    {
        return _requestRepository.GetById(requestId);
    }
    
    public ConversationRequest SendConversationRequest(int requesterId, int receiverId, ConversationType type, string? groupName = null, List<int>? additionalUserIds = null,string? message = null)
    {
        // Check for existing request - use the overload that includes groupName for group requests
        ConversationRequest? existingRequest;
        if (type == ConversationType.Group && !string.IsNullOrEmpty(groupName))
        {
            existingRequest = _requestRepository.GetRequest(requesterId, receiverId, type, groupName);
        }
        else
        {
            existingRequest = _requestRepository.GetRequest(requesterId, receiverId, type);
        }
        
        if (existingRequest != null)
        {
            throw new InvalidOperationException("A pending request already exists.");
        }

        var requester = _userRepository.GetById(requesterId);
        var receiver = _userRepository.GetById(receiverId);

        if (requester == null || receiver == null)
        {
            throw new ArgumentException("Invalid user IDs");
        }

        var request = new ConversationRequest
        {
            RequesterId = requesterId,
            ReceiverId = receiverId,
            ConversationType = type,
            GroupName = groupName,
            AdditionalUserIds = additionalUserIds,
            Message = message,
            Status = ConversationRequestStatus.Pending,
            RequestedAt = DateTime.UtcNow
        };

        _requestRepository.Create(request);
        
        // Send SignalR notification
        _hubContext.Clients.Group($"user_{receiverId}")
            .SendAsync("ReceiveConversationRequest", new
            {
                RequestId = request.Id,
                RequesterId = requesterId,
                RequesterName = requester.DisplayName ?? requester.UserName,
                ConversationType = type,
                GroupName = groupName,
                Message = message,
                RequestedAt = request.RequestedAt
            });
            
        return request;
    }

    public void AcceptRequest(int requestId, int userId)
    {
        var request = _requestRepository.GetById(requestId);

        if (request == null)
        {
            throw new ArgumentException("Request not found");
        }

        if (request.ReceiverId != userId)
        {
            throw new UnauthorizedAccessException("You can only accept requests sent to you");
        }

        if (request.Status != ConversationRequestStatus.Pending)
        {
            throw new InvalidOperationException("Request is no longer pending");
        }

        request.Status = ConversationRequestStatus.Accepted;
        request.RespondedAt = DateTime.UtcNow;
        _requestRepository.Update(request);

        if (request.ConversationType == ConversationType.Private)
        {
            _conversationService.CreatePrivateConversation(request.RequesterId, request.ReceiverId);
        }
        else if (request.ConversationType == ConversationType.Group)
        {
            var userIds = request.AdditionalUserIds ?? new List<int>();
            userIds.Add(request.ReceiverId); 
            
            _conversationService.CreateGroupConversation(
                request.GroupName ?? "Group Chat",
                request.RequesterId,
                userIds
            );
        }
    }

    public void DeclineRequest(int requestId, int userId)
    {
        var request = _requestRepository.GetById(requestId);

        if (request == null)
        {
            throw new ArgumentException("Request not found");
        }

        if (request.ReceiverId != userId)
        {
            throw new UnauthorizedAccessException("You can only decline requests sent to you");
        }

        if (request.Status != ConversationRequestStatus.Pending)
        {
            throw new InvalidOperationException("Request is no longer pending");
        }

        request.Status = ConversationRequestStatus.Declined;
        request.RespondedAt = DateTime.UtcNow;
        _requestRepository.Update(request);
    }

    public void CancelRequest(int requestId, int userId)
    {
        var request = _requestRepository.GetById(requestId);

        if (request == null)
        {
            throw new ArgumentException("Request not found");
        }

        if (request.RequesterId != userId)
        {
            throw new UnauthorizedAccessException("You can only cancel your own requests");
        }

        if (request.Status != ConversationRequestStatus.Pending)
        {
            throw new InvalidOperationException("Request is no longer pending");
        }

        _requestRepository.Delete(request);
    }

    public List<ConversationRequest> GetPendingRequests(int userId)
    {
        return _requestRepository.GetPendingRequestsForUser(userId);
    }

    public List<ConversationRequest> GetSentRequests(int userId)
    {
        return _requestRepository.GetSentRequestsByUser(userId);
    }

    public bool HasPendingRequest(int requesterId, int receiverId, ConversationType type)
    {
        var request = _requestRepository.GetRequest(requesterId, receiverId, type);
        return request != null;
    }
}