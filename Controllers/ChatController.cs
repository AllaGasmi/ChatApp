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
    private readonly IUserRepository _userRepository;
    private readonly INotificationService _notificationService;

    public ChatController(IConversationService conversationService,IMessageRepository messageRepository,IConversationRepository conversationRepository, IUserRepository userRepository,IFriendshipRepository friendshipRepo,IFriendshipService friendshipService,IConversationRequestService conversationRequestService,IConversationRequestRepository requestRepository, INotificationService notificationService)
    {
        _conversationService = conversationService;
        _messageRepository = messageRepository;
        _conversationRepository = conversationRepository;
        _userRepository = userRepository;
        _friendshipRepo=friendshipRepo;
        _friendshipService=friendshipService;
        _conversationRequestService=conversationRequestService;
        _requestRepository=requestRepository;
        _notificationService=notificationService;
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

        var currentUser = _userRepository.GetById(userId);
        var otherUser = _userRepository.GetById(otherUserId);
        
        if (!areFriends)
        {
            try
            {
                _conversationRequestService.SendConversationRequest(
                    userId, 
                    otherUserId, 
                    ConversationType.Private
                );

                _notificationService.SendNotification(otherUserId, new Notification() {
                    Type = NotificationType.RequestReceived,
                    Message = currentUser.DisplayName + " wants to start a private conversation with you."
                });
                
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

        userIds = userIds
            .Where(id => id > 0 && id != userId)
            .Distinct()
            .ToList();

        if (!userIds.Any())
        {
            TempData["Error"] = "No valid users selected.";
            return RedirectToAction("NewGroup");
        }

        var currentUser = _userRepository.GetById(userId);
        var currentUserName = currentUser?.DisplayName ?? currentUser?.UserName ?? "Someone";

        var friendIds = new List<int>();
        var nonFriendIds = new List<int>();

        foreach (var otherUserId in userIds)
        {
            if (_friendshipService.AreFriends(userId, otherUserId))
            {
                friendIds.Add(otherUserId);
            }
            else
            {
                nonFriendIds.Add(otherUserId);
            }
        }

        try
        {
            if (friendIds.Any())
            {
                var conversation = _conversationService.CreateGroupConversation(groupName, userId, friendIds);
                
                // Send invitations to non-friends to join the existing group
                int sentInvitations = 0;
                foreach (var nonFriendId in nonFriendIds)
                {
                    try
                    {
                        // Get current participants (friends + creator)
                        var currentParticipants = conversation.Participants
                            .Select(p => p.UserId)
                            .Where(id => id != userId) // Don't include creator in the list
                            .ToList();

                        _conversationRequestService.SendConversationRequest(
                            userId,
                            nonFriendId,
                            ConversationType.Group,
                            groupName,
                            currentParticipants,
                            $"You've been invited to join '{groupName}'"
                        );

                        // Send notification
                        _notificationService.SendNotification(nonFriendId, new Notification() 
                        {
                            Type = NotificationType.RequestReceived,
                            Message = $"{currentUserName} invited you to join \"{groupName}\"."
                        });
                        
                        sentInvitations++;
                    }
                    catch (InvalidOperationException)
                    {
                        // Request already exists for this user
                        continue;
                    }
                }

                // Set success message based on what happened
                if (sentInvitations > 0)
                {
                    TempData["Success"] = $"Group created with {friendIds.Count} friend(s)! " +
                                        $"{sentInvitations} invitation(s) sent to other users.";
                }
                else if (nonFriendIds.Count > 0)
                {
                    TempData["Success"] = $"Group created with {friendIds.Count} friend(s)! " +
                                        $"Some invitations couldn't be sent (pending requests may already exist).";
                }
                else
                {
                    TempData["Success"] = $"Group '{groupName}' created successfully with {friendIds.Count} member(s)!";
                }
                
                return RedirectToAction("Conversation", new { id = conversation.Id });
            }
            // If we ONLY have non-friends, send invitations and wait for acceptance
            else if (nonFriendIds.Any())
            {
                // Create invitations for ALL non-friends
                // Each invitation includes ALL OTHER non-friends as additional users
                int createdRequests = 0;
                
                foreach (var nonFriendId in nonFriendIds)
                {
                    try
                    {
                        // List all OTHER non-friends as additional users
                        var otherNonFriends = nonFriendIds
                            .Where(id => id != nonFriendId)
                            .ToList();

                        _conversationRequestService.SendConversationRequest(
                            userId,
                            nonFriendId,
                            ConversationType.Group,
                            groupName,
                            otherNonFriends,
                            $"You've been invited to join '{groupName}'"
                        );

                        // Send notification
                        _notificationService.SendNotification(nonFriendId, new Notification() 
                        {
                            Type = NotificationType.RequestReceived,
                            Message = $"{currentUserName} invited you to join \"{groupName}\"."
                        });
                        
                        createdRequests++;
                    }
                    catch (InvalidOperationException)
                    {
                        // Request already exists for this user
                        continue;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending invitation to user {nonFriendId}: {ex.Message}");
                        continue;
                    }
                }

                if (createdRequests > 0)
                {
                    TempData["Success"] = $"Group invitations sent to {createdRequests} user(s). " +
                                        $"The group will be created when the first invitation is accepted.";
                }
                else
                {
                    TempData["Error"] = "Could not send any invitations. Pending requests may already exist for all users.";
                }
                
                return RedirectToAction("Requests");
            }
            else
            {
                TempData["Error"] = "No valid users selected.";
                return RedirectToAction("NewGroup");
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error creating group: {ex.Message}";
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
            var currentUser = _userRepository.GetById(actorId);
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
                    
                    _notificationService.SendNotification(userId, new Notification() 
                    {
                        Type = NotificationType.RequestReceived,
                        Message = $"{currentUser.DisplayName} invited you to join \"{conversation.Name}\"."
                    });
                    
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
            var requesterGroups = _conversationService.GetUserConversations(request.RequesterId)
                .Where(c => c.Type == ConversationType.Group && c.Name == request.GroupName)
                .ToList();
            
            conversation = FindMatchingGroup(request, requesterGroups);
            
            if (conversation == null)
            {
                var userIds = new List<int>();
                
                if (request.ReceiverId != request.RequesterId)
                {
                    userIds.Add(request.ReceiverId);
                }
                
                conversation = _conversationService.CreateGroupConversation(
                    request.GroupName ?? "Group Chat",
                    request.RequesterId,
                    userIds
                );
                
            }
            else
            {
                if (!_conversationService.IsUserInConversation(conversation.Id, request.ReceiverId))
                {
                    _conversationService.AddParticipant(
                        conversation.Id,
                        request.RequesterId,
                        request.ReceiverId
                    );
                }
                
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
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult LeaveGroup(int conversationId)
    {
        try
        {
            int userId = GetCurrentUserId();
            _conversationService.LeaveGroup(conversationId, userId);
            
            TempData["Success"] = "You have left the group successfully.";
            return RedirectToAction("Index");
        }
        catch (UnauthorizedAccessException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction("Conversation", new { id = conversationId });
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction("Conversation", new { id = conversationId });
        }
        catch (Exception ex)
        {
            TempData["Error"] = "An error occurred while leaving the group.";
            return RedirectToAction("Conversation", new { id = conversationId });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConversation(int conversationId)
    {
        try
        {
            int userId = GetCurrentUserId();
            _conversationService.DeleteConversation(conversationId, userId);
            
            TempData["Success"] = "Conversation deleted successfully.";
            return RedirectToAction("Index");
        }
        catch (UnauthorizedAccessException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction("Conversation", new { id = conversationId });
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction("Conversation", new { id = conversationId });
        }
        catch (Exception ex)
        {
            TempData["Error"] = "An error occurred while deleting the conversation.";
            return RedirectToAction("Conversation", new { id = conversationId });
        }
    }
}