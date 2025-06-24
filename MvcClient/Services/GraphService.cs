using Microsoft.Graph;
using Microsoft.Graph.Models;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication;

namespace MvcClient.Services
{
    /// <summary>
    /// Microsoft Graph Service for accessing Azure AD user data
    /// 
    /// AUTHENTICATION FLOWS:
    /// 1. App-Only (Client Credentials): Uses app registration's client secret to access Graph API
    ///    - Used for: GetUsersAsync, GetUserByIdAsync, SearchUsersAsync, GetCurrentUserAsync
    ///    - Permissions: Application permissions (e.g., User.Read.All)
    ///    - Runs as the application, not as a specific user
    /// 
    /// 2. Delegated (On-Behalf-Of): Would use user's token to access Graph API as that user
    ///    - Intended for: GetCurrentUserWithUserTokenAsync 
    ///    - Permissions: Delegated permissions (e.g., User.Read)
    ///    - Runs as the authenticated user
    ///    - STATUS: Not implemented - requires OBO flow setup
    /// 
    /// CURRENT LIMITATION:
    /// IdentityServer tokens cannot be used directly with Microsoft Graph.
    /// To use delegated permissions, we need to implement the On-Behalf-Of (OBO) flow
    /// which exchanges the IdentityServer token for a Microsoft Graph token.
    /// </summary>
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
        }        // Method to get Graph client with user's token via On-Behalf-Of flow
        private async Task<GraphServiceClient?> GetUserGraphClientAsync()
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext?.User?.Identity?.IsAuthenticated == true)
                {
                    // Get the user's Identity Server access token
                    var identityServerToken = await httpContext.GetTokenAsync("access_token");
                    
                    if (!string.IsNullOrEmpty(identityServerToken))
                    {
                        _logger.LogInformation("Found Identity Server token, attempting On-Behalf-Of flow");
                        
                        // For now, we'll use app-only authentication as user tokens from Identity Server
                        // cannot be directly used with Microsoft Graph without On-Behalf-Of flow setup
                        // This would require additional Azure AD configuration
                        
                        _logger.LogWarning("On-Behalf-Of flow not implemented yet, falling back to app token");
                        return null;
                    }
                    else
                    {
                        _logger.LogWarning("No access token found in user context");
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user Graph client");
                return null;
            }
        }        // Method to get current user using THEIR OWN token (delegated permissions)
        public async Task<User?> GetCurrentUserWithUserTokenAsync()
        {
            try
            {
                _logger.LogInformation("Attempting to get current user with user token");

                var userGraphClient = await GetUserGraphClientAsync();
                if (userGraphClient != null)
                {
                    // This would use USER'S TOKEN with delegated permissions
                    var me = await userGraphClient.Me
                        .GetAsync(requestConfiguration =>
                        {
                            requestConfiguration.QueryParameters.Select = new[] { "id", "displayName", "mail", "userPrincipalName", "jobTitle", "department", "officeLocation" };
                        });

                    _logger.LogInformation($"Retrieved current user via user token: {me?.DisplayName}");
                    return me;
                }

                // On-Behalf-Of flow not implemented, show clear message
                _logger.LogWarning("User token flow not available - On-Behalf-Of flow not implemented");
                throw new InvalidOperationException("Delegated user token access requires On-Behalf-Of (OBO) flow setup. Currently using app-only authentication.");
            }
            catch (InvalidOperationException)
            {
                // Re-throw our specific error
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user with user token");
                throw new InvalidOperationException("Unable to access Microsoft Graph with user token. On-Behalf-Of flow required.", ex);
            }
        }
    }
}
