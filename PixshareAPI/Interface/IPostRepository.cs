using PixshareAPI.Models;

namespace PixshareAPI.Interface
{
    public interface IPostRepository
    {
        Task CreatePostAsync(Post post, IFormFile movieFile);

        Task<IEnumerable<object>> GetAllPostsAsync();

        Task<object> GetPostByIdAsync(string postId);

        Task AddCommentAsync(string postId, CommentRequest request);

        Task EditCommentAsync(string postId, string commentId, EditCommentRequest request);

        Task DeleteCommentAsync(string postId, string commentId);

        Task<IEnumerable<object>> GetPostsByUserIdAsync(string userId);

        Task EditPostAsync(string postId, Post updatedPost);

        Task DeletePostAsync(string postId, string userId);

        Task<int> GetCommentsCount(string postId);

    }


}
