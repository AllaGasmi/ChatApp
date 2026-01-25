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
    private readonly IFriendshipRepository _friendshipRepo;
    private readonly IFriendshipService _friendshipService;
    private readonly IConversationRequestService _conversationRequestService;
    private readonly IMessageRepository _messageRepository;
    private readonly IConversationRequestRepository _requestRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserRepository _userRepository;

    public ChatController(IConversationService conversationService,IMessageRepository messageRepository,IConversationRepository conversationRepository,UserManager<ApplicationUser> userManager, IUserRepository userRepository,IFriendshipRepository friendshipRepo,IFriendshipService friendshipService,IConversationRequestService conversationRequestService,IConversationRequestRepository requestRepository)
    {
        _conversationService = conversationService;
        _messageRepository = messageRepository;
        _conversationRepository = conversationRepository;
        _userManager = userManager;
        _userRepository = userRepository;
        _friendshipRepo=friendshipRepo;
        _friendshipService=friendshipService;
        _conversationRequestService=conversationRequestService;
        _requestRepository=requestRepository;
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
            users = _userRepository.GetUsersWhoAllowPrivateChats(currentUserId).Where(u => u.Id != currentUserId).ToList();
        }
        else
        {
            users = _friendshipRepo.GetAllFriends(currentUserId);
        }
        
        ViewBag.Users = users;
        ViewBag.ShowAll = showAll;
        ViewBag.FriendsCount = _friendshipRepo.GetAllFriends(currentUserId).Count;
        
        return View();
    }

    [HttpPost]
    public IActionResult CreatePrivate(int otherUserId)
    {
        int userId = GetCurrentUserId();
        var existingConversation = _conversationService.GetPrivateConversation(userId, otherUserId);
    
        if (existingConversation != null)
        {
            return RedirectToAction("Conversation", new { id = existingConversation.Id });
        }
        bool areFriends = _friendshipService.AreFriends(userId, otherUserId);
        
        if (!areFriends)
        {
            try
            {
                _conversationRequestService.SendConversationRequest(
                    userId, 
                    otherUserId, 
                    ConversationType.Private
                );
                TempData["Success"] = "Conversation request sent! Waiting for acceptance.";
            }
            catch (InvalidOperationException)
            {
                TempData["Info"] = "You already have a pending request with this user.";
            }
            return RedirectToAction("Index");
        }
        var conversation = _conversationService.CreatePrivateConversation(userId, otherUserId);

        return RedirectToAction("Conversation", new { id = conversation.Id });
    }

    public async Task<IActionResult> NewGroup(bool showAll = false)
    {
        int currentUserId = GetCurrentUserId();
        
        List<ApplicationUser> users;
        
        if (showAll)
        {
            users = _userRepository.GetUsersWhoAllowGroupChats().Where(u => u.Id != currentUserId).ToList();
        }
        else
        {
            users = _friendshipRepo.GetAllFriends(currentUserId);
        }
        
        ViewBag.Users = users;
        ViewBag.ShowAll = showAll;
        ViewBag.FriendsCount = _friendshipRepo.GetAllFriends(currentUserId).Count;
        
        return View();
    }

    [HttpPost]
    public IActionResult CreateGroup(string groupName, List<int> userIds)
    {
        int userId = GetCurrentUserId();

        if (string.IsNullOrWhiteSpace(groupName))
        {
            TempData["Error"] = "Group name is required";
            return RedirectToAction("NewGroup");
        }

        if (userIds == null || !userIds.Any())
        {
            TempData["Error"] = "Please select at least one user to create a group.";
            return RedirectToAction("NewGroup");
        }

        var nonFriendUsers = new List<int>();
        var friendUsers = new List<int>();

        foreach (var otherUserId in userIds)
        {
            if (_friendshipService.AreFriends(userId, otherUserId))
            {
                friendUsers.Add(otherUserId);
            }
            else
            {
                nonFriendUsers.Add(otherUserId);
            }
        }

        // Case 1: At least one friend - create group immediately with friends
        if (friendUsers.Any())
        {
            try
            {
                var conversation = _conversationService.CreateGroupConversation(groupName, userId, friendUsers);
                
                // Send requests to non-friends
                int sentInvitations = 0;
                foreach (var nonFriendId in nonFriendUsers)
                {
                    try
                    {
                        var currentConversation = _conversationRepository.GetConversationWithDetails(conversation.Id);
                        var allCurrentParticipants = currentConversation.Participants.Select(p => p.UserId).ToList();
                        
                        _conversationRequestService.SendConversationRequest(
                            userId,
                            nonFriendId,
                            ConversationType.Group,
                            groupName,
                            allCurrentParticipants.Where(p => p != userId).ToList(),
                            "You've been invited to join this group"
                        );
                        sentInvitations++;
                    }
                    catch (InvalidOperationException)
                    {
                        // Request already exists, continue
                        continue;
                    }
                }

                if (sentInvitations > 0)
                {
                    TempData["Success"] = $"Group created with {friendUsers.Count} friend(s)! Invitations sent to {sentInvitations} other user(s).";
                }
                else if (nonFriendUsers.Any())
                {
                    TempData["Success"] = $"Group created with {friendUsers.Count} friend(s)! Some invitations couldn't be sent (they may already exist).";
                }
                else
                {
                    TempData["Success"] = "Group created successfully!";
                }
                
                return RedirectToAction("Conversation", new { id = conversation.Id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error creating group: {ex.Message}";
                return RedirectToAction("NewGroup");
            }
        }
        // Case 2: All selected users are non-friends
        else if (nonFriendUsers.Any())
        {
            var createdRequests = new List<int>();
            
            // The first user gets a request with all other non-friends in AdditionalUserIds
            var additionalUsers = nonFriendUsers.Skip(1).ToList();
            
            try
            {
                var firstRequest = _conversationRequestService.SendConversationRequest(
                    userId,
                    nonFriendUsers.First(),
                    ConversationType.Group,
                    groupName,
                    additionalUsers,
                    "You've been invited to join this group"
                );
                createdRequests.Add(firstRequest.Id);
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = $"Cannot send invitation: {ex.Message}";
                return RedirectToAction("NewGroup");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error sending invitation: {ex.Message}";
                return RedirectToAction("NewGroup");
            }
            
            // Send requests to other non-friends
            for (int i = 1; i < nonFriendUsers.Count; i++)
            {
                try
                {
                    // Build list of other users (all non-friends except the current one)
                    var otherUsers = new List<int>(nonFriendUsers);
                    otherUsers.RemoveAt(i); // Remove current user from their own additional users list
                    
                    var request = _conversationRequestService.SendConversationRequest(
                        userId,
                        nonFriendUsers[i],
                        ConversationType.Group,
                        groupName,
                        otherUsers,
                        "You've been invited to join this group"
                    );
                    createdRequests.Add(request.Id);
                }
                catch (InvalidOperationException)
                {
                    // Request already exists, continue
                    continue;
                }
                catch (Exception ex)
                {
                    // Log error but continue with other invitations
                    Console.WriteLine($"Error sending invitation to user {nonFriendUsers[i]}: {ex.Message}");
                    continue;
                }
            }
            
            if (createdRequests.Count > 0)
            {
                TempData["Success"] = $"Group invitations sent to {createdRequests.Count} user(s). The group will be created when at least one invitation is accepted.";
            }
            else
            {
                TempData["Error"] = "Could not send any invitations. Pending requests may already exist.";
            }
            
            return RedirectToAction("Requests");
        }
        else
        {
            TempData["Error"] = "No users selected.";
            return RedirectToAction("NewGroup");
        }
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
            bool areFriends = _friendshipService.AreFriends(actorId, userId);
            
            if (!areFriends)
            {
                var conversation = _conversationRepository.GetConversationWithDetails(conversationId);
                var existingParticipants = conversation.Participants.Select(p => p.UserId).ToList();
                
                try
                {
                    _conversationRequestService.SendConversationRequest(
                        actorId,
                        userId,
                        ConversationType.Group,
                        conversation.Name,
                        existingParticipants,
                        "You've been invited to join this group"
                    );
                    
                    TempData["Success"] = "Invitation sent! User will be added when they accept.";
                }
                catch (InvalidOperationException)
                {
                    TempData["Info"] = "You already have a pending invitation for this user.";
                }
            }
            else
            {
                _conversationService.AddParticipant(conversationId, actorId, userId);
                TempData["Success"] = "Participant added successfully";
            }
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
            availableUsers = _userRepository.GetUsersWhoAllowGroupChats()
                    .Where(u => !participantIds.Contains(u.Id) && u.Id != userId)
                    .ToList();
        }
        else
        {
            var friends = _friendshipRepo.GetAllFriends(userId);
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
    public IActionResult Requests()
    {
        int userId = GetCurrentUserId();
        
        var pendingRequests = _conversationRequestService.GetPendingRequests(userId);
        var sentRequests = _conversationRequestService.GetSentRequests(userId);
        
        ViewBag.PendingRequests = pendingRequests;
        ViewBag.SentRequests = sentRequests;
        
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AcceptRequest(int requestId)
    {
        int userId = GetCurrentUserId();
        
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

        Conversation conversation = null;

        if (request.ConversationType == ConversationType.Private)
        {
            conversation = _conversationService.GetPrivateConversation(request.RequesterId, request.ReceiverId);
            
            if (conversation == null)
            {
                conversation = _conversationService.CreatePrivateConversation(request.RequesterId, request.ReceiverId);
            }
        }
        else if (request.ConversationType == ConversationType.Group)
        {
            // Check if the requester already has a group with this name
            var requesterGroups = _conversationService.GetUserConversations(request.RequesterId)
                .Where(c => c.Type == ConversationType.Group && c.Name == request.GroupName)
                .ToList();
            
            conversation = FindMatchingGroup(request, requesterGroups);
            
            if (conversation == null)
            {
                // Group doesn't exist yet - create it with ONLY the requester and receiver
                // DO NOT add AdditionalUserIds - they need to accept their own requests
                var userIds = new List<int>();
                
                // Only add the receiver (person accepting the request)
                if (request.ReceiverId != request.RequesterId)
                {
                    userIds.Add(request.ReceiverId);
                }
                
                // Create the group with only requester (added automatically) and receiver
                conversation = _conversationService.CreateGroupConversation(
                    request.GroupName ?? "Group Chat",
                    request.RequesterId,
                    userIds
                );
                
                // Note: AdditionalUserIds should NOT be added here
                // They will be added when they accept their own individual requests
            }
            else
            {
                // Group already exists - just add this user if not already in it
                if (!_conversationService.IsUserInConversation(conversation.Id, request.ReceiverId))
                {
                    _conversationService.AddParticipant(
                        conversation.Id,
                        request.RequesterId,
                        request.ReceiverId
                    );
                }
                
                // DO NOT add AdditionalUserIds here
                // They each have their own pending requests and should accept individually
            }
        }

        TempData["Success"] = "Request accepted successfully.";
        return RedirectToAction("Requests");
    }
        
    private Conversation? FindMatchingGroup(ConversationRequest request, List<Conversation> requesterGroups)
    {
        if (requesterGroups.Count == 0) return null;
        
        if (requesterGroups.Count == 1)
        {
            return requesterGroups[0];
        }
        
        Conversation? bestMatch = null;
        int maxMatches = 0;
        
        foreach (var group in requesterGroups)
        {
            var participantIds = group.Participants.Select(p => p.UserId).ToList();
            
            int matches = 0;
            if (request.AdditionalUserIds != null)
            {
                matches = request.AdditionalUserIds.Count(id => participantIds.Contains(id));
            }
            
            if (matches > maxMatches)
            {
                maxMatches = matches;
                bestMatch = group;
            }
        }
        
        return bestMatch;
    }
        
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeclineRequest(int requestId)
    {
        try
        {
            int userId = GetCurrentUserId();
            _conversationRequestService.DeclineRequest(requestId, userId);
            
            TempData["Success"] = "Request declined.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        
        return RedirectToAction("Requests");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CancelRequest(int requestId)
    {
        try
        {
            int userId = GetCurrentUserId();
            _conversationRequestService.CancelRequest(requestId, userId);
            
            TempData["Success"] = "Request cancelled.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        
        return RedirectToAction("Requests");
    }
}