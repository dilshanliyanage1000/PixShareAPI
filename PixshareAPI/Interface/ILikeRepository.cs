using PixshareAPI.Models;

namespace PixshareAPI.Interface
{
    public interface ILikeRepository
    {
        Task AddLike(string postId, string userId);

        Task RemoveLike(string postId, string userId);

        Task<List<Like>> GetLikes(string postId);
    }
}
