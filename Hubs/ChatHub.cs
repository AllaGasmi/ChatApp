using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using ChatAppProj.ServiceContracts;
using ChatAppProj.Models;

//[Authorize]
public class ChatHub : Hub
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConversationService _conversationService;

    public ChatHub(UserManager<ApplicationUser> userManager,IConversationService conversationService)
    {
        _userManager = userManager;
        _conversationService = conversationService;
    }

    private int GetUserId()
    {
        // return int.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var httpContext = Context.GetHttpContext();
        var userIdString = httpContext.Request.Query["userId"].ToString();

        if (int.TryParse(userIdString, out int userId))
            return userId;

        // fallback
        return 1;
    }
    public override async Task OnConnectedAsync()
    {
        int userId = GetUserId();

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

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        int userId = GetUserId();

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

        var messages = _conversationService.SendMessage(
            conversationId,
            userId,
            content
        );

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


}

