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
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var users = await _graphService.GetUsersAsync();
                return View(users?.Value ?? new List<Microsoft.Graph.Models.User>());
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
        }
    }
}
