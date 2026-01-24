using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ChatAppProj.ServiceContracts;
using ChatAppProj.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using ChatAppProj.RepositoryContracts;

namespace ChatAppProj.Controllers;

[Authorize]
public class ChatController : Controller
{
    private readonly IConversationService _conversationService;
    private readonly IMessageRepository _messageRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly UserManager<ApplicationUser> _userManager;

    public ChatController(IConversationService conversationService,IMessageRepository messageRepository,IConversationRepository conversationRepository,UserManager<ApplicationUser> userManager)
    {
        _conversationService = conversationService;
        _messageRepository = messageRepository;
        _conversationRepository = conversationRepository;
        _userManager = userManager;
    }

    private int GetCurrentUserId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }

    public IActionResult Index()
    {
        int userId = GetCurrentUserId();
        var conversations = _conversationService.GetUserConversations(userId);
        
        ViewBag.UserId = userId;
        ViewBag.UserName = User.Identity!.Name;
        
        return View(conversations);
    }

    public IActionResult Conversation(int id)
    {
        int userId = GetCurrentUserId();

        if (!_conversationService.IsUserInConversation(id, userId))
        {
            return Forbid();
        }

        var conversation = _conversationRepository.GetConversationWithDetails(id);
        if (conversation == null)
        {
            return NotFound();
        }

        var messages = _messageRepository.GetConversationMessages(id);

        ViewBag.UserId = userId;
        ViewBag.UserName = User.Identity!.Name;
        ViewBag.ConversationId = id;
        ViewBag.ConversationName = conversation.Name ?? GetPrivateChatName(conversation, userId);
        ViewBag.Messages = messages;

        return View(conversation);
    }
    public async Task<IActionResult> NewPrivate()
    {
        var users = _userManager.Users.ToList();
        int currentUserId = GetCurrentUserId();
        
        ViewBag.Users = users.Where(u => u.Id != currentUserId).ToList();
        return View();
    }

    [HttpPost]
    public IActionResult CreatePrivate(int otherUserId)
    {
        int userId = GetCurrentUserId();

        var conversation = _conversationService.CreatePrivateConversation(userId, otherUserId);

        return RedirectToAction("Conversation", new { id = conversation.Id });
    }

    public async Task<IActionResult> NewGroup()
    {
        var users = _userManager.Users.ToList();
        int currentUserId = GetCurrentUserId();
        
        ViewBag.Users = users.Where(u => u.Id != currentUserId).ToList();
        return View();
    }

    [HttpPost]
    public IActionResult CreateGroup(string groupName, List<int> userIds)
    {
        int userId = GetCurrentUserId();

        if (string.IsNullOrWhiteSpace(groupName))
        {
            ModelState.AddModelError("", "Group name is required");
            return RedirectToAction("NewGroup");
        }

        var conversation = _conversationService.CreateGroupConversation(groupName, userId, userIds);

        return RedirectToAction("Conversation", new { id = conversation.Id });
    }

    private string GetPrivateChatName(Conversation conversation, int currentUserId)
    {
        var otherUser = conversation.Participants
            .FirstOrDefault(p => p.UserId != currentUserId)?.User;
        
        return otherUser?.DisplayName ?? "Unknown User";
    }
}