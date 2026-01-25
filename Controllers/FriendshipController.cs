using System.Security.Claims;
using ChatAppProj.Models;
using ChatAppProj.RepositoryContracts;
using ChatAppProj.ServiceContracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatAppProj.Controllers;

[Authorize]
public class FriendshipController : Controller {
    private readonly IFriendshipRepository _friendshipRepository;
    private readonly IFriendshipService _friendshipService;
    private readonly IUserRepository _userRepository;

    public FriendshipController(IFriendshipRepository friendshipRepository, IFriendshipService friendshipService, IUserRepository userRepository) {
        _friendshipRepository = friendshipRepository;
        _friendshipService = friendshipService;
        _userRepository = userRepository;
    }

    private int GetCurrentUserId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
    
    public IActionResult Find() {
        int userId = GetCurrentUserId();

        var available = _userRepository.GetUsersWhoAllowRequests();
        available = available.Where(u => u.Id != userId).ToList();
        
        ViewBag.Available = available;
        
        if (TempData["Message"] != null) {
            ViewBag.Message = TempData["Message"];
            ViewBag.MessageType = TempData["MessageType"] ?? "info";
        }
        
        return View();
    }

    [HttpPost]
    public IActionResult SendRequest(int receiverId) {
        int senderId = GetCurrentUserId();
        var receiver = _userRepository.GetById(receiverId);

        var response = _friendshipService.SendFriendRequest(senderId, receiverId);
        
        string message;
        string messageType = "info";
        
        switch (response)
        {
            case SendFriendRequestResponse.Ok:
                message = "Friend request sent to " + receiver.DisplayName + ".";
                messageType = "success";
                break;
            case SendFriendRequestResponse.RequestsDisabled:
                message = receiver.DisplayName + " has disabled friend requests.";
                messageType = "warning";
                break;
            case SendFriendRequestResponse.AlreadyFriends:
                message = "You and " + receiver.DisplayName + " are already friends.";
                messageType = "info";
                break;
            case SendFriendRequestResponse.AlreadySent:
                message = "You have already sent " + receiver.DisplayName + " a friend request.";
                messageType = "info";
                break;
            case SendFriendRequestResponse.Blocked:
                message = receiver.DisplayName + " has blocked you.";
                messageType = "danger";
                break;
            case SendFriendRequestResponse.Declined:
                message = receiver.DisplayName + " has declined your request.";
                messageType = "warning";
                break;
            default:
                message = "An error occurred while sending the request.";
                messageType = "danger";
                break;
        }
        
        TempData["Message"] = message;
        TempData["MessageType"] = messageType;
        
        return RedirectToAction("Find");
    }

    public IActionResult Pending() {
        int userId = GetCurrentUserId();

        var pending = _friendshipRepository.GetAllPending(userId);

        var sent = pending.Where(f => f.RequesterId == userId);
        var received = pending.Where(f => f.AddresseeId == userId);
        
        ViewBag.SentRequests = sent;
        ViewBag.ReceivedRequests = received;
        
        ViewBag.DeletionHappened = TempData["DeletionHappened"] == null ? false : TempData["DeletionHappened"];
        
        return View();
    }

    [HttpPost]
    public IActionResult CancelRequest(int requestId) {
        _friendshipService.CancelRequest(requestId);

        TempData["DeletionHappened"] = true;
        
        return RedirectToAction("Pending");
    }
    
    [HttpPost]
    public IActionResult AcceptRequest(int requestId) {
        _friendshipService.AcceptRequest(requestId);
        
        TempData["SuccessMessage"] = "Friend request accepted!";
        
        return RedirectToAction("Pending");
    }

    [HttpPost]
    public IActionResult DeclineRequest(int requestId) {
        _friendshipService.DeclineRequest(requestId);
        
        TempData["SuccessMessage"] = "Friend request declined.";
        
        return RedirectToAction("Pending");
    }

    public IActionResult Users(string? search) {
        List<ApplicationUser> users;
        
        if (search == null) {
            users = _userRepository.SearchUsers("");
        } else {
            users = _userRepository.SearchUsers(search);
        }
        
        users = users.Where(u => u.Id != GetCurrentUserId()).ToList();

        ViewBag.Users = users;
        
        return View();
    }

    public IActionResult BlockUser(int blockedId) {
        int userId = GetCurrentUserId();
        var blocked = _userRepository.GetById(blockedId);

        var response = _friendshipService.BlockUser(userId, blockedId);
        
        string message;
        string messageType = "info";

        switch (response) {
            case (BlockUserResponse.Ok):
                message = blocked.DisplayName + " has been blocked.";
                messageType = "success";
                break;
            case (BlockUserResponse.AlreadyBlocked):
                message = blocked.DisplayName + " has already been blocked.";
                messageType = "warning";
                break;
            case (BlockUserResponse.AlreadyFriends):
                message = "You and " + blocked.DisplayName + " are already friends.";
                messageType = "info";
                break;
            default:
                message = "An error occurred while sending the request.";
                messageType = "danger";
                break;
        }
        
        TempData["Message"] = message;
        TempData["MessageType"] = messageType;
        
        return RedirectToAction("Users");
    }
}