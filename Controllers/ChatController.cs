using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ChatAppProj.ServiceContracts;
using ChatAppProj.Models;
using System.Security.Claims;
using ChatAppProj.Repositories;
using Microsoft.AspNetCore.Identity;
using ChatAppProj.RepositoryContracts;

namespace ChatAppProj.Controllers;

[Authorize]
public class ChatController : Controller
{
    private readonly IConversationService _conversationService;
    private readonly IFriendshipRepository _friendshipService;
    private readonly IMessageRepository _messageRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserRepository _userRepository;

    public ChatController(IConversationService conversationService,IMessageRepository messageRepository,IConversationRepository conversationRepository,UserManager<ApplicationUser> userManager, IUserRepository userRepository,IFriendshipRepository friendshipService)
    {
        _conversationService = conversationService;
        _messageRepository = messageRepository;
        _conversationRepository = conversationRepository;
        _userManager = userManager;
        _userRepository = userRepository;
        _friendshipService=friendshipService;
    }

    private int GetCurrentUserId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }

    public async Task<IActionResult> Index()
    {
        int userId = GetCurrentUserId();
         _conversationService.EnsureAiConversation(userId);
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
        bool isGroup = conversation.Type == ConversationType.Group;

        var currentParticipant = conversation.Participants
            .FirstOrDefault(p => p.UserId == userId);

        bool isCreator = currentParticipant?.Role == ConversationRole.Creator;
        bool isAdminOrCreator = isCreator ||
                                currentParticipant?.Role == ConversationRole.Admin;

        ViewBag.UserId = userId;
        ViewBag.UserName = User.Identity!.Name;
        ViewBag.ConversationId = id;
        ViewBag.ConversationName =
            conversation.Name ?? GetPrivateChatName(conversation, userId);
        ViewBag.Messages = messages;

        ViewBag.IsGroup = isGroup;
        ViewBag.IsCreator = isCreator;
        ViewBag.IsAdminOrCreator = isAdminOrCreator;

        return View(conversation);
    }
    public async Task<IActionResult> NewPrivate(bool showAll = false)
    {
        int currentUserId = GetCurrentUserId();
        
        List<ApplicationUser> users;
        
        if (showAll)
        {
            users = _userManager.Users.Where(u => u.Id != currentUserId).ToList();
        }
        else
        {
            users = _friendshipService.GetAllFriends(currentUserId);
        }
        
        ViewBag.Users = users;
        ViewBag.ShowAll = showAll;
        ViewBag.FriendsCount = _friendshipService.GetAllFriends(currentUserId).Count;
        
        return View();
    }

    [HttpPost]
    public IActionResult CreatePrivate(int otherUserId)
    {
        int userId = GetCurrentUserId();

        var conversation = _conversationService.CreatePrivateConversation(userId, otherUserId);

        return RedirectToAction("Conversation", new { id = conversation.Id });
    }

    public async Task<IActionResult> NewGroup(bool showAll = false)
    {
        int currentUserId = GetCurrentUserId();
        
        List<ApplicationUser> users;
        
        if (showAll)
        {
            users = _userManager.Users.Where(u => u.Id != currentUserId).ToList();
        }
        else
        {
            users = _friendshipService.GetAllFriends(currentUserId);
        }
        
        ViewBag.Users = users;
        ViewBag.ShowAll = showAll;
        ViewBag.FriendsCount = _friendshipService.GetAllFriends(currentUserId).Count;
        
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
        
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddParticipant(int conversationId, int userId)
    {
        try
        {
            int actorId = GetCurrentUserId();
            _conversationService.AddParticipant(conversationId, actorId, userId);
            
            TempData["Success"] = "Participant added successfully";
        }
        catch (UnauthorizedAccessException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        
        return RedirectToAction("Conversation", new { id = conversationId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RemoveParticipant(int conversationId, int userId)
    {
        try
        {
            int actorId = GetCurrentUserId();
            _conversationService.RemoveParticipant(conversationId, actorId, userId);
            
            TempData["Success"] = "Participant removed successfully";
        }
        catch (UnauthorizedAccessException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        
        return RedirectToAction("Conversation", new { id = conversationId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AssignAdmin(int conversationId, int userId)
    {
        try
        {
            int actorId = GetCurrentUserId();
            _conversationService.AssignAdmin(conversationId, actorId, userId);
            
            TempData["Success"] = "Admin assigned successfully";
        }
        catch (UnauthorizedAccessException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        
        return RedirectToAction("Conversation", new { id = conversationId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateGroupInfo(int conversationId, string groupName, IFormFile? groupPicture)
    {
        try
        {
            int actorId = GetCurrentUserId();
            
            _conversationService.UpdateGroupInfo(conversationId, actorId, groupName, groupPicture);
            
            TempData["Success"] = "Group information updated successfully";
        }
        catch (UnauthorizedAccessException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        
        return RedirectToAction("Conversation", new { id = conversationId });
    }

    public async Task<IActionResult> GetAvailableUsers(int conversationId, bool showAll = false)
    {
        int userId = GetCurrentUserId();
        
        if (!_conversationService.IsUserInConversation(conversationId, userId))
        {
            return Forbid();
        }
        
        var conversation = _conversationRepository.GetConversationWithDetails(conversationId);
        var participantIds = conversation.Participants.Select(p => p.UserId).ToList();
        
        List<ApplicationUser> availableUsers;
        
        if (showAll)
        {
            availableUsers = _userManager.Users
                .Where(u => !participantIds.Contains(u.Id))
                .ToList();
        }
        else
        {
            var friends = _friendshipService.GetAllFriends(userId);
            availableUsers = friends
                .Where(f => !participantIds.Contains(f.Id))
                .ToList();
        }
        
        var result = availableUsers.Select(u => new 
                                            { 
                                                u.Id, 
                                                u.UserName, 
                                                u.DisplayName, 
                                                u.IsOnline,
                                                u.ProfilePicture
                                            }).ToList();
        
        return Json(result);
    }
}