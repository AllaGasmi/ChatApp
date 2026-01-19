
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;


[Authorize]  
public class ChatHub : Hub
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    
    public ChatHub(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }
    
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (!string.IsNullOrEmpty(userId))
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.IsOnline = true;
                user.LastSeen = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
                
                
                await Clients.Others.SendAsync("UserOnline", int.Parse(userId), user.UserName);
            }
        }
        
        await base.OnConnectedAsync();
    }
    
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (!string.IsNullOrEmpty(userId))
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.IsOnline = false;
                user.LastSeen = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
                
                // Notifier les amis que l'utilisateur est hors ligne
                await Clients.Others.SendAsync("UserOffline", int.Parse(userId), user.UserName);
            }
        }
        
        await base.OnDisconnectedAsync(exception);
    }
    
    public async Task SendMessage(int conversationId, string content)
    {
        var userId = int.Parse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        
        var message = new Message
        {
            ConversationId = conversationId,
            SenderId = userId,
            Content = content,
            SentAt = DateTime.UtcNow,
            IsAI = false
        };
        
        _context.Messages.Add(message);
        await _context.SaveChangesAsync();
        

        await Clients.Group($"conversation_{conversationId}")
            .SendAsync("ReceiveMessage", message);
    }
    
    public async Task JoinConversation(int conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
    }
    
    public async Task LeaveConversation(int conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
    }
}