using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MvcClient.Services;

namespace MvcClient.Controllers
{
    [Authorize]
    public class UsersController : Controller
    {
        private readonly IGraphService _graphService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IGraphService graphService, ILogger<UsersController> logger)
        {
            _graphService = graphService;
            _logger = logger;
        }        public async Task<IActionResult> Index()
        {
            try
            {
                var users = await _graphService.GetUsersAsync();
                return View(users?.Value ?? new List<Microsoft.Graph.Models.User>());
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Graph API Error"))
            {
                _logger.LogError(ex, "Microsoft Graph permission error");
                ViewBag.Error = $"Permission Error: {ex.Message}. Please check Azure AD app permissions.";
                ViewBag.PermissionHelp = true;
                return View(new List<Microsoft.Graph.Models.User>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading users page");
                ViewBag.Error = "Unable to load users. Please check your permissions.";
                return View(new List<Microsoft.Graph.Models.User>());
            }
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var user = await _graphService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound();
                }
                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading user details for {id}");
                return NotFound();
            }
        }

        [HttpPost]
        public async Task<IActionResult> Search(string searchQuery)
        {
            if (string.IsNullOrEmpty(searchQuery))
            {
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var users = await _graphService.SearchUsersAsync(searchQuery);
                ViewBag.SearchQuery = searchQuery;
                return View("Index", users?.Value ?? new List<Microsoft.Graph.Models.User>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error searching users with query: {searchQuery}");
                ViewBag.Error = "Unable to search users. Please try again.";
                return View("Index", new List<Microsoft.Graph.Models.User>());
            }
        }        public async Task<IActionResult> Me()
        {
            try
            {
                // Try using user's own token first (delegated permissions)
                var currentUser = await _graphService.GetCurrentUserWithUserTokenAsync();
                ViewBag.TokenType = "User Token (Delegated Permissions)";
                return View(currentUser);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("On-Behalf-Of"))
            {
                _logger.LogInformation("User token flow not available, falling back to app token");
                
                try
                {
                    // Fallback to app token method
                    var currentUser = await _graphService.GetCurrentUserAsync();
                    if (currentUser == null)
                    {
                        ViewBag.Error = "Unable to retrieve your profile information.";
                        ViewBag.TokenInfo = "User not found in directory.";
                        return View();
                    }
                    
                    ViewBag.TokenType = "App Token (Application Permissions)";
                    ViewBag.InfoMessage = "Note: Displaying your profile using app-only authentication. For true delegated access, On-Behalf-Of (OBO) flow setup is required.";
                    return View(currentUser);
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogError(fallbackEx, "Error in fallback method");
                    ViewBag.Error = "Unable to retrieve your profile information.";
                    ViewBag.TokenInfo = ex.Message;
                    return View();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading current user profile");
                ViewBag.Error = "Unable to load your profile. Please try again.";
                ViewBag.TokenInfo = ex.Message;
                return View();
            }
        }
    }
}
