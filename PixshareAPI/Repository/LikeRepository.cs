using Amazon.DynamoDBv2.DataModel;
using Amazon.Runtime.Internal;
using Amazon.S3;
using PixshareAPI.Interface;
using PixshareAPI.Models;
using System.ComponentModel.Design;

namespace PixshareAPI.Repository
{
    public class LikeRepository : ILikeRepository
    {
        private readonly IDynamoDBContext _dynamoDbContext;

        public LikeRepository(IDynamoDBContext dynamoDbContext)
        {
            _dynamoDbContext = dynamoDbContext;
        }

        public async Task AddLike(string postId, string userId)
        {
            var post = await _dynamoDbContext.LoadAsync<Post>(postId);

            if (post == null) throw new Exception("Post not found");

            var user = await _dynamoDbContext.LoadAsync<User>(userId);

            if (user == null) throw new Exception("User not found");

            var newLike = new Like
            {
                UserId = userId
            };

            post.Likes?.Add(newLike);

            await _dynamoDbContext.SaveAsync(post);
        }

        public async Task RemoveLike(string postId, string userId)
        {
            var post = await _dynamoDbContext.LoadAsync<Post>(postId);

            if (post == null) throw new Exception("Post not found");

            var like = post.Likes?.FirstOrDefault(l => l.UserId == userId);

            if (like == null) throw new Exception("Comment not found");

            post.Likes?.Remove(like);

            await _dynamoDbContext.SaveAsync(post);
        }

        public async Task<List<Like>> GetLikes(string postId)
        {
            try
            {
                var post = await _dynamoDbContext.LoadAsync<Post>(postId);

                if (post == null) throw new Exception("Post not found");

                return post.Likes ?? [];
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving post: {ex.Message}");

                return [];
            }
        }
    }
}
