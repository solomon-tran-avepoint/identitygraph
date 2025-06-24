using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace MvcClient.Services
{
    public interface IGraphService
    {
        Task<UserCollectionResponse?> GetUsersAsync();
        Task<User?> GetUserByIdAsync(string userId);
        Task<UserCollectionResponse?> SearchUsersAsync(string searchQuery);
    }
}
