using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using ChatAppProj.ServiceContracts;
using ChatAppProj.Models;
using ChatAppProj.RepositoryContracts;
using Microsoft.VisualBasic;

[Authorize]
public class ChatHub : Hub
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConversationService _conversationService;
    private readonly IConversationRequestService _conversationRequestService;
    private readonly IUserRepository _userRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly INotificationService _notificationService;
    private static readonly Dictionary<int, string> _userConnections = new();

    public ChatHub(UserManager<ApplicationUser> userManager,IConversationService conversationService,IConversationRequestService conversationRequestService,IUserRepository userRepository, IConversationRepository conversationRepository, INotificationService notificationService)
    {
        _userManager = userManager;
        _conversationService = conversationService;
        _conversationRequestService = conversationRequestService;
        _userRepository = userRepository;
        _conversationRepository = conversationRepository;
        _notificationService = notificationService;
    }

    private int GetUserId()
    {
        return int.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);
        // var httpContext = Context.GetHttpContext();
        // var userIdString = httpContext.Request.Query["userId"].ToString();

        // if (int.TryParse(userIdString, out int userId))
        //     return userId;

        // // fallback
        // return 1;
    }
    public override async Task OnConnectedAsync()
    {
        int userId = GetUserId();
        _userConnections[userId] = Context.ConnectionId;
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user != null)
        {
            user.IsOnline = true;
            user.LastSeen = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            await Clients.Others.SendAsync("UserOnline", userId, user.UserName);
        }

        var conversations = _conversationService.GetUserConversations(userId);

        foreach (var conv in conversations)
        {
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                $"conversation_{conv.Id}"
            );
        }
        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            $"user_{userId}"
        );
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        int userId = GetUserId();
        _userConnections.Remove(userId);
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user != null)
        {
            user.IsOnline = false;
            user.LastSeen = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            await Clients.Others.SendAsync("UserOffline", userId, user.UserName);
        }

        await base.OnDisconnectedAsync(exception);
    }
    public async Task SendMessage(int conversationId, string content)
    {
        int userId = GetUserId();
        var user = _userRepository.GetById(userId);

        var messages = _conversationService.SendMessage(
            conversationId,
            userId,
            content
        );

        var conversation = _conversationRepository.GetById(conversationId);
        var participants = conversation.Participants;

        foreach (var participant in participants) {
            var currentParticipantId = participant.UserId;

            if (userId != currentParticipantId) {
                string msg;
                if (conversation.Type == ConversationType.Private) {
                    msg = "New message from " + user.DisplayName  + " : \"" + content + "\".";
                }
                else {
                    msg = "New message in " + conversation.Name + " : \"" + content + "\".";
                }
                
                _notificationService.SendNotification(currentParticipantId, new Notification() {
                    Message = msg,
                    Type = NotificationType.Message
                });
            }
        }

        foreach (var msg in messages)
        {
            await Clients.Group($"conversation_{conversationId}")
                .SendAsync("ReceiveMessage", msg);
        }
    }

    public async Task JoinConversation(int conversationId)
    {
        int userId = GetUserId();

        if (!_conversationService.IsUserInConversation(conversationId, userId))
            throw new HubException("Access denied");

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            $"conversation_{conversationId}"
        );
    }
    public async Task LeaveConversation(int conversationId)
    {
        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            $"conversation_{conversationId}"
        );
    }
    public async Task SendConversationRequestNotification(int requestId)
    {
        var request = _conversationRequestService.GetRequestById(requestId);
        if (request == null) return;

        await Clients.Group($"user_{request.ReceiverId}")
            .SendAsync("ReceiveConversationRequest", new
            {
                RequestId = request.Id,
                RequesterId = request.RequesterId,
                RequesterName = request.Requester.DisplayName ?? request.Requester.UserName,
                ConversationType = request.ConversationType,
                GroupName = request.GroupName,
                Message = request.Message,
                RequestedAt = request.RequestedAt
            });
    }
    public async Task NotifyUser(int userId, string method, object data)
    {
        if (_userConnections.TryGetValue(userId, out string connectionId))
        {
            await Clients.Client(connectionId).SendAsync(method, data);
        }
        else
        {
            // If user is offline, store notification in database
        }
    }

    public async Task RequestResponse(int requestId, bool accepted)
    {
        int userId = GetUserId();
        
        try
        {
            if (accepted)
            {
                _conversationRequestService.AcceptRequest(requestId, userId);
                
                var request = _conversationRequestService.GetRequestById(requestId);
                if (request != null)
                {
                    await NotifyUser(request.RequesterId, "RequestAccepted", new
                    {
                        RequestId = request.Id,
                        ReceiverName = request.Receiver.DisplayName ?? request.Receiver.UserName
                    });
                }
            }
            else
            {
                _conversationRequestService.DeclineRequest(requestId, userId);
                
                var request = _conversationRequestService.GetRequestById(requestId);
                if (request != null)
                {
                    await NotifyUser(request.RequesterId, "RequestDeclined", new
                    {
                        RequestId = request.Id,
                        ReceiverName = request.Receiver.DisplayName ?? request.Receiver.UserName
                    });
                }
            }
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("RequestError", ex.Message);
        }
    }

}

