using PixshareAPI.Models;

namespace PixshareAPI.Interface
{
    public interface ILikeRepository
    {
        Task AddLike(string postId, string userId);

    }
}
