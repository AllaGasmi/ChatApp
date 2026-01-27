using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using ChatAppProj.Models;
using ChatAppProj.DTO;
using ChatAppProj.Repositories;
using ChatAppProj.RepositoryContracts;
using ChatAppProj.ServiceContracts;
using Microsoft.EntityFrameworkCore;

namespace ChatAppProj.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        private readonly IFriendshipService _friendshipService;
        private readonly IConversationService _conversationService;
        private readonly IUserRepository _userRepository;
        private readonly IFriendshipRepository _friendshipRepository;


        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IFriendshipService friendshipService,
            IConversationService conversationService,
            IUserRepository userRepository,
            IFriendshipRepository friendshipRepository)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _friendshipService = friendshipService;
            _conversationService = conversationService;
            _userRepository = userRepository;
            _friendshipRepository = friendshipRepository;
        }


        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterDto model) {
            if (!ModelState.IsValid)
                return View(model);

            var user = new ApplicationUser {
                UserName = model.Email,
                Email = model.Email,
                DisplayName = model.DisplayName ?? model.Email.Split('@')[0],
                ProfilePicture = model.ProfilePicture ?? "/default-avatar.png",
                CreatedAt = DateTime.UtcNow,
                IsOnline = true,
                LastSeen = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded) {
                await _signInManager.SignInAsync(user, isPersistent: false);
            
                var aiUser = await _userManager.FindByEmailAsync(SystemUsers.AiEmail);

                if (aiUser != null) {
                    var aiFriendship = new Friendship() {
                        Addressee = user,
                        AddresseeId = user.Id,
                        Requester = aiUser,
                        RequesterId = aiUser.Id,
                        RequestedAt = DateTime.UtcNow,
                        RespondedAt = DateTime.UtcNow,
                        Status = FriendshipStatus.Accepted
                    };
                
                    _friendshipRepository.Create(aiFriendship);
                }
                
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginDto model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "User not found");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
                user.IsOnline = true;
                await _userManager.UpdateAsync(user);
                return RedirectToAction("Index", "Home");

            if (result.IsLockedOut)
            {
                ModelState.AddModelError("", "Account locked. Try again later.");
                return View(model);
            }

            ModelState.AddModelError("", "Invalid login attempt");
            return View(model);
        }


        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        // GET: /Account/Profile
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login");

            var model = new UpdateProfileDto
            {
                DisplayName = user.DisplayName,
                ProfilePicture = user.ProfilePicture
            };

            return View(model);
        }

        // POST: /Account/Profile
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(UpdateProfileDto model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login");

            user.DisplayName = model.DisplayName;
            user.ProfilePicture = model.ProfilePicture;
            user.Bio = model.Bio;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                ViewBag.Message = "Profile updated successfully";
                return View(model);
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Configuration()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login");

            var config = _userRepository.GetUserConfiguration(user.Id);

            return View(config);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Configuration(UserConfiguration model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return RedirectToAction("Login");

                user = await _userManager.Users
                    .Include(u => u.UserConfiguration)
                    .FirstOrDefaultAsync(u => u.Id == user.Id);

                if (user.UserConfiguration == null)
                {
                    user.UserConfiguration = new UserConfiguration
                    {
                        AllowRequest = model.AllowRequest,
                        AllowBeingAddedToGroup = model.AllowBeingAddedToGroup,
                        AllowOnlyFriendsChat = model.AllowOnlyFriendsChat
                    };
                }
                else
                {
                    user.UserConfiguration.AllowRequest = model.AllowRequest;
                    user.UserConfiguration.AllowBeingAddedToGroup = model.AllowBeingAddedToGroup;
                    user.UserConfiguration.AllowOnlyFriendsChat = model.AllowOnlyFriendsChat;
                }

                await _userManager.UpdateAsync(user);

                ViewBag.SuccessMessage = "Your settings have been updated successfully!";

                return View(user.UserConfiguration);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred while saving your settings: {ex.Message}");
                return View(model);
            }
        }

        [Authorize]
        public async Task<IActionResult> Friends()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var friends = _friendshipRepository.GetAllFriends(currentUser.Id);
            ViewBag.Friends = friends;

            return View();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> RemoveFriend(int friendId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var currentUserId = currentUser.Id;

            _friendshipService.UnfriendUser(currentUserId, friendId);

            return RedirectToAction("Friends");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ViewProfile(string userId)
        {
            try
            {
                
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                    return RedirectToAction("Login", "Account");

                if (string.IsNullOrEmpty(userId))
                    return RedirectToAction("Profile");

                var targetUser = await _userManager.FindByIdAsync(userId);
                if (targetUser == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("Index", "Home");
                }

                bool isOwnProfile = targetUser.Id == currentUser.Id;

                var pendingRequests =  _friendshipRepository.GetAllPending(currentUser.Id);

                bool hasPendingRequest = pendingRequests.Any(f =>
                    (f.RequesterId == currentUser.Id && f.AddresseeId == targetUser.Id) ||
                    (f.RequesterId == targetUser.Id && f.AddresseeId == currentUser.Id));


                bool isFriend = _friendshipService.AreFriends(currentUser.Id, targetUser.Id);
                bool hasBlocked = _friendshipService.HasBlocked(currentUser.Id, targetUser.Id);
                bool isBlocked = _friendshipService.HasBeenBlockedBy(currentUser.Id, targetUser.Id);

                var viewModel = new ViewProfileDto
                {
                    UserId = targetUser.Id.ToString(),
                    DisplayName = targetUser.DisplayName ?? targetUser.UserName,
                    Email = isOwnProfile ? targetUser.Email : null,
                    ProfilePicture = targetUser.ProfilePicture ?? "https://via.placeholder.com/150",
                    Bio = targetUser.Bio,
                    CreatedAt = targetUser.CreatedAt,
                    Status = targetUser.IsOnline ? "Online" : "Offline",
                    IsFriend = isFriend,
                    HasBlocked = hasBlocked,
                    IsBlocked = isBlocked,
                    HasPendingRequest = hasPendingRequest,
                    CanSendFriendRequest = !isOwnProfile && !isFriend && !hasPendingRequest
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Home");
            }
        }

    }
}
