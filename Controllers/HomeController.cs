using System.Security.Claims;
using ChatAppProj.Models;
using ChatAppProj.ServiceContracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConversationService _conversationService;

    public HomeController(ILogger<HomeController> logger, UserManager<ApplicationUser> userManager,IConversationService conversationService)
    {
        _logger = logger;
        _userManager = userManager;
        _conversationService = conversationService;
    }

    public async Task<IActionResult> Index()
    {
        if (User.Identity?.IsAuthenticated ?? false)
        {
            // Get current user
            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                
                var stats = _conversationService.GetDashboardStats(userId);
                var recentConversations = _conversationService.GetRecentConversations(userId, 3);

                ViewBag.UserName = user.DisplayName ?? user.UserName;
                ViewBag.UserAvatar = user.ProfilePicture ?? "https://via.placeholder.com/80";
                ViewBag.UserEmail = user.Email;
                ViewBag.Stats = stats;
                ViewBag.RecentConversations = recentConversations;

                return View("Index"); // Shows authenticated user home with stats
            }
        }

        return View("Landing"); // Shows public landing page
    }

    // Add this for the About Us page
    public IActionResult About()
    {
        return View();
    }
    
}
