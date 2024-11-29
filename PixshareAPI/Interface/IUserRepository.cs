using PixshareAPI.Models;

namespace PixshareAPI.Interface
{
    public interface IUserRepository
    {
        Task<User?> GetUserByUsernameAsync(string username);

        Task<User?> GetUserByEmailAsync(string email);

        Task<User?> GetUserByIdAsync(string userId);

        Task SaveUserAsync(User user);

        Task UpdateUserAsync(User user);

        Task DeleteUserAsync(string userId);
    }

}
