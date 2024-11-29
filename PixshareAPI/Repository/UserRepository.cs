using Amazon.DynamoDBv2.DataModel;
using PixshareAPI.Interface;
using PixshareAPI.Models;

namespace PixshareAPI.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly IDynamoDBContext _context;

        public UserRepository(IDynamoDBContext context)
        {
            _context = context;
        }

        public async Task<User?> GetUserByUsernameAsync(string username) =>
            await _context.LoadAsync<User>(username);

        public async Task<User?> GetUserByEmailAsync(string email) =>
            (await _context.ScanAsync<User>(new List<ScanCondition>
            {
            new ScanCondition("EmailAddress", Amazon.DynamoDBv2.DocumentModel.ScanOperator.Equal, email)
            }).GetRemainingAsync()).FirstOrDefault();

        public async Task<User?> GetUserByIdAsync(string userId) =>
            await _context.LoadAsync<User>(userId);

        public async Task SaveUserAsync(User user) =>
            await _context.SaveAsync(user);

        public async Task UpdateUserAsync(User user) =>
            await _context.SaveAsync(user);

        public async Task DeleteUserAsync(string userId)
        {
            var user = await _context.LoadAsync<User>(userId);
            if (user != null)
            {
                await _context.DeleteAsync(user);
            }
        }
    }

}
