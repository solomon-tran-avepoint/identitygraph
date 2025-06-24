using Microsoft.Graph;
using Microsoft.Graph.Models;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Identity.Web;

namespace MvcClient.Services
{    /// <summary>
    /// Microsoft Graph Service for accessing Azure AD user data
    /// 
    /// AUTHENTICATION FLOWS:
    /// 1. App-Only (Client Credentials): Uses app registration's client secret to access Graph API
    ///    - Used for: GetUsersAsync, GetUserByIdAsync, SearchUsersAsync, GetCurrentUserAsync
    ///    - Permissions: Application permissions (e.g., User.Read.All)
    ///    - Runs as the application, not as a specific user
    ///    - STATUS: ✅ Fully implemented and working
    /// 
    /// 2. Delegated (On-Behalf-Of): Would use user's token to access Graph API as that user
    ///    - Intended for: GetCurrentUserWithUserTokenAsync, GetUsersWithUserTokenAsync
    ///    - Permissions: Delegated permissions (e.g., User.Read)
    ///    - Runs as the authenticated user
    ///    - STATUS: ❌ Not compatible with IdentityServer setup
    /// 
    /// CURRENT LIMITATION:
    /// IdentityServer tokens cannot be used with Microsoft Graph OBO flow due to JWT header incompatibility.
    /// IdentityServer issues tokens for local application use, while Graph API requires Azure AD-issued tokens.
    /// 
    /// SOLUTIONS FOR DELEGATED PERMISSIONS:
    /// 1. Replace IdentityServer with direct Azure AD authentication
    /// 2. Implement custom token exchange mechanism
    /// 3. Use separate authentication flow for Graph API operations
    /// 4. Continue using app-only permissions (current working solution)
    /// </summary>
    public class GraphService : IGraphService
    {
        private readonly GraphServiceClient _graphServiceClient;
        private readonly ILogger<GraphService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;

        public GraphService(IConfiguration configuration, ILogger<GraphService> logger, IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            
            // For app-only authentication (fallback)
            var clientId = configuration["AzureAd:ClientId"];
            var clientSecret = configuration["AzureAd:ClientSecret"];
            var tenantId = configuration["AzureAd:TenantId"];

            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            _graphServiceClient = new GraphServiceClient(credential);
        }

        public async Task<UserCollectionResponse?> GetUsersAsync()
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
        }        // Method to get Graph client with user's token using interactive authentication
        private Task<GraphServiceClient?> GetUserGraphClientAsync()
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext?.User?.Identity?.IsAuthenticated == true)
                {
                    _logger.LogInformation("IdentityServer tokens cannot be used directly with Microsoft Graph");
                    _logger.LogInformation("OBO flow requires tokens issued by Azure AD, not IdentityServer");
                    
                    // For true delegated permissions, we would need:
                    // 1. Configure the app to authenticate directly with Azure AD (not through IdentityServer)
                    // 2. Or implement a custom token exchange mechanism
                    // 3. Or use a different authentication flow
                    
                    _logger.LogWarning("User token authentication not available with current IdentityServer setup");
                }
                
                return Task.FromResult<GraphServiceClient?>(null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error attempting to create user Graph client");
                return Task.FromResult<GraphServiceClient?>(null);
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

                // User token flow not available with IdentityServer setup
                _logger.LogWarning("User token flow not available - IdentityServer tokens cannot be used with Microsoft Graph OBO flow");
                throw new InvalidOperationException("IdentityServer tokens cannot be used directly with Microsoft Graph. For delegated permissions, the application would need to authenticate directly with Azure AD instead of through IdentityServer.");
            }
            catch (InvalidOperationException)
            {
                // Re-throw our specific error
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user with user token");
                throw new InvalidOperationException("Unable to access Microsoft Graph with user token. IdentityServer tokens are incompatible with Graph API OBO flow.", ex);
            }
        }

        // Method to get users using user's token (delegated permissions)
        public async Task<UserCollectionResponse?> GetUsersWithUserTokenAsync()
        {
            try
            {
                _logger.LogInformation("Getting users with user token via OBO flow");

                var userGraphClient = await GetUserGraphClientAsync();
                if (userGraphClient != null)
                {
                    // This uses USER'S TOKEN with delegated permissions
                    var users = await userGraphClient.Users
                        .GetAsync(requestConfiguration =>
                        {
                            requestConfiguration.QueryParameters.Select = new[] { "id", "displayName", "mail", "userPrincipalName", "jobTitle", "department", "officeLocation" };
                            requestConfiguration.QueryParameters.Top = 10; // Limit to 10 users for testing
                        });

                    _logger.LogInformation($"Retrieved {users?.Value?.Count ?? 0} users via user token");
                    return users;
                }

                // Fallback to app-only authentication
                _logger.LogWarning("User token not available, falling back to app-only authentication");
                return await GetUsersAsync();
            }
            catch (Microsoft.Graph.Models.ODataErrors.ODataError odataEx)
            {
                _logger.LogError(odataEx, "Microsoft Graph OData Error with user token: {ErrorCode} - {ErrorMessage}", 
                    odataEx.Error?.Code, odataEx.Error?.Message);
                
                // If it's a permission error with user token, provide helpful message
                if (odataEx.Error?.Code == "Forbidden" || odataEx.Error?.Code == "InsufficientPermissions")
                {
                    throw new InvalidOperationException($"Graph API Permission Error with user token: {odataEx.Error?.Code} - {odataEx.Error?.Message}. The user may not have sufficient permissions to read users.", odataEx);
                }
                
                throw new InvalidOperationException($"Graph API Error with user token: {odataEx.Error?.Code} - {odataEx.Error?.Message}", odataEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users with user token, falling back to app token");
                // Fallback to app-only authentication
                return await GetUsersAsync();
            }
        }
    }
}
