using Microsoft.Graph;
using Microsoft.Graph.Models;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication;

namespace MvcClient.Services
{
    public class GraphService : IGraphService
    {
        private readonly GraphServiceClient _graphServiceClient;
        private readonly ILogger<GraphService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public GraphService(IConfiguration configuration, ILogger<GraphService> logger, IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            
            // For now, keep using app-only authentication
            // TODO: Switch to on-behalf-of flow to use user's token
            var clientId = configuration["AzureAd:ClientId"];
            var clientSecret = configuration["AzureAd:ClientSecret"];
            var tenantId = configuration["AzureAd:TenantId"];

            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            _graphServiceClient = new GraphServiceClient(credential);
        }public async Task<UserCollectionResponse?> GetUsersAsync()
        {
            try
            {
                _logger.LogInformation("Getting all users from Microsoft Graph");
                
                var users = await _graphServiceClient.Users
                    .GetAsync(requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.Select = new[] { "id", "displayName", "mail", "userPrincipalName", "jobTitle", "department", "officeLocation" };
                        requestConfiguration.QueryParameters.Top = 10; // Limit to 10 users for testing
                    });

                _logger.LogInformation($"Retrieved {users?.Value?.Count ?? 0} users from Microsoft Graph");
                return users;
            }
            catch (Microsoft.Graph.Models.ODataErrors.ODataError odataEx)
            {
                _logger.LogError(odataEx, "Microsoft Graph OData Error: {ErrorCode} - {ErrorMessage}", 
                    odataEx.Error?.Code, odataEx.Error?.Message);
                throw new InvalidOperationException($"Graph API Error: {odataEx.Error?.Code} - {odataEx.Error?.Message}", odataEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users from Microsoft Graph");
                throw;
            }
        }

        public async Task<User?> GetUserByIdAsync(string userId)
        {
            try
            {
                _logger.LogInformation($"Getting user {userId} from Microsoft Graph");
                
                var user = await _graphServiceClient.Users[userId]
                    .GetAsync(requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.Select = new[] { "id", "displayName", "mail", "userPrincipalName", "jobTitle", "department", "officeLocation" };
                    });

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting user {userId} from Microsoft Graph");
                throw;
            }
        }

        public async Task<UserCollectionResponse?> SearchUsersAsync(string searchQuery)
        {
            try
            {
                _logger.LogInformation($"Searching users with query: {searchQuery}");
                
                var users = await _graphServiceClient.Users
                    .GetAsync(requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.Filter = $"startswith(displayName,'{searchQuery}') or startswith(mail,'{searchQuery}') or startswith(userPrincipalName,'{searchQuery}')";
                        requestConfiguration.QueryParameters.Select = new[] { "id", "displayName", "mail", "userPrincipalName", "jobTitle", "department", "officeLocation" };
                    });

                _logger.LogInformation($"Found {users?.Value?.Count ?? 0} users matching query: {searchQuery}");
                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error searching users with query: {searchQuery}");
                throw;
            }
        }

        public async Task<User?> GetCurrentUserAsync()
        {
            try
            {
                _logger.LogInformation("Getting current user information");

                // For now, we'll use the app token to get user by their UPN from the claims
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext?.User?.Identity?.IsAuthenticated == true)
                {
                    var userPrincipalName = httpContext.User.FindFirst("preferred_username")?.Value ??
                                          httpContext.User.FindFirst("upn")?.Value ??
                                          httpContext.User.FindFirst("email")?.Value;

                    if (!string.IsNullOrEmpty(userPrincipalName))
                    {
                        var users = await _graphServiceClient.Users
                            .GetAsync(requestConfiguration =>
                            {
                                requestConfiguration.QueryParameters.Filter = $"userPrincipalName eq '{userPrincipalName}'";
                                requestConfiguration.QueryParameters.Select = new[] { "id", "displayName", "mail", "userPrincipalName", "jobTitle", "department", "officeLocation" };
                            });

                        return users?.Value?.FirstOrDefault();
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user information");
                throw;
            }
        }
    }
}
