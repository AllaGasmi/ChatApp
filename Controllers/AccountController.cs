using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using ChatAppProj.Models;
using ChatAppProj.DTO;
using ChatAppProj.ServiceContracts;

namespace ChatAppProj.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        private readonly IFriendshipService _friendshipService;
        private readonly IConversationService _conversationService;


        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IFriendshipService friendshipService,
            IConversationService conversationService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _friendshipService = friendshipService;
            _conversationService = conversationService;
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
        public async Task<IActionResult> Register(RegisterDto model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                DisplayName = model.DisplayName ?? model.Email.Split('@')[0],
                ProfilePicture = model.ProfilePicture ?? "/default-avatar.png",
                CreatedAt = DateTime.UtcNow,
                IsOnline = true,
                LastSeen = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {

                
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Profile");
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

        
    }
}
