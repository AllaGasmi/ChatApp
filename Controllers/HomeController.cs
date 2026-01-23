using ChatAppProj.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(ILogger<HomeController> logger, UserManager<ApplicationUser> userManager)
    {
        _logger = logger;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        if (User.Identity.IsAuthenticated)
        {
            // Get current user
            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                ViewBag.UserName = user.DisplayName ?? user.UserName;
                ViewBag.UserAvatar = user.ProfilePicture ?? "https://via.placeholder.com/80";
                ViewBag.UserEmail = user.Email;
            }

            return View("Index"); // Shows authenticated user home
        }

        return View("Landing"); // Shows public landing page (if you have one)
    }

    // Add this for the About Us page
    public IActionResult About()
    {
        return View();
    }
}
