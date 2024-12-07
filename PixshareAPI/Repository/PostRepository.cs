using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.S3.Transfer;
using Amazon.S3;
using PixshareAPI.Interface;
using PixshareAPI.Models;
using Amazon.S3.Model;
using System.Collections.Generic;

namespace PixshareAPI.Repository
{
    public class PostRepository : IPostRepository
    {
        private readonly IDynamoDBContext _dynamoDbContext;
        private readonly IAmazonS3 _s3Client;
        private const string _s3bucketName = "pixshare-api";
        private readonly ILikeRepository _likeRepository;

        public PostRepository(IDynamoDBContext dynamoDbContext, IAmazonS3 s3Client, ILikeRepository likeRepository)
        {
            _dynamoDbContext = dynamoDbContext;
            _s3Client = s3Client;
            _likeRepository = likeRepository;
        }

        public async Task CreatePostAsync(Post post, IFormFile movieFile)
        {
            if (movieFile != null && movieFile.Length > 0)
            {
                var newPostId = Guid.NewGuid().ToString();
                post.PostId = newPostId;

                try
                {
                    using var stream = movieFile.OpenReadStream();
                    var uploadRequest = new TransferUtilityUploadRequest
                    {
                        InputStream = stream,
                        Key = newPostId,
                        BucketName = _s3bucketName,
                        ContentType = movieFile.ContentType,
                        CannedACL = S3CannedACL.PublicRead
                    };

                    var fileTransferUtility = new TransferUtility(_s3Client);
                    await fileTransferUtility.UploadAsync(uploadRequest);
                    post.S3Url = $"https://{_s3bucketName}.s3.amazonaws.com/{newPostId}";
                }
                catch (AmazonS3Exception ex)
                {
                    throw new Exception($"Error uploading file to S3: {ex.Message}");
                }
            }

            post.PostedDate = DateTime.UtcNow;
            await _dynamoDbContext.SaveAsync(post);
        }

        public async Task<IEnumerable<object>> GetAllPostsAsync()
        {
            var posts = await _dynamoDbContext.ScanAsync<Post>(new List<ScanCondition>()).GetRemainingAsync();

            var enrichedPosts = new List<object>();

            foreach (var post in posts)
            {
                var user = await _dynamoDbContext.LoadAsync<User>(post.UserId);

                var likesList = await _likeRepository.GetLikes(post.PostId);

                var commentsCount = await GetCommentsCount(post.PostId);

                var likesCount = likesList.Count();

                enrichedPosts.Add(new
                {
                    post.PostId,
                    post.UserId,
                    post.PostCaption,
                    post.Location,
                    post.PostedDate,
                    post.S3Url,
                    post.Comments,
                    likesList,
                    likesCount,
                    commentsCount,
                    FullName = user?.FullName ?? "Unknown User",
                    Username = user?.Username ?? "Unknown User"
                });
            }

            return enrichedPosts;
        }


        public async Task<object?> GetPostByIdAsync(string postId)
        {
            try
            {
                var post = await _dynamoDbContext.LoadAsync<Post>(postId);

                if (post == null)
                {
                    return null;
                }

                var user = await _dynamoDbContext.LoadAsync<User>(post.UserId);

                var likesList = await _likeRepository.GetLikes(post.PostId);

                var commentsCount = await GetCommentsCount(post.PostId);

                var likesCount = likesList.Count();

                return new
                {
                    post.PostId,
                    post.UserId,
                    post.PostCaption,
                    post.Location,
                    post.PostedDate,
                    post.S3Url,
                    post.Comments,
                    likesList,
                    likesCount,
                    commentsCount,
                    FullName = user?.FullName ?? "Unknown User",
                    Username = user?.Username ?? "Unknown User"
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving post: {ex.Message}");

                return null;
            }
        }


        public async Task<IEnumerable<object>> GetPostsByUserIdAsync(string userId)
        {
            try
            {
                var posts = await _dynamoDbContext.ScanAsync<Post>(new List<ScanCondition>
                {
                    new("UserId", ScanOperator.Equal, userId)
                }).GetRemainingAsync();

                var enrichedPosts = new List<object>();

                foreach (var post in posts)
                {
                    var user = await _dynamoDbContext.LoadAsync<User>(post.UserId);

                    var likesList = await _likeRepository.GetLikes(post.PostId);

                    var commentsCount = await GetCommentsCount(post.PostId);

                    var likesCount = likesList.Count();

                    enrichedPosts.Add(new
                    {
                        post.PostId,
                        post.UserId,
                        post.PostCaption,
                        post.Location,
                        post.PostedDate,
                        post.S3Url,
                        post.Comments,
                        likesList,
                        likesCount,
                        commentsCount,
                        FullName = user?.FullName ?? "Unknown User",
                        Username = user?.Username ?? "Unknown User"
                    });
                }

                return enrichedPosts;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving posts: {ex.Message}");
                throw;
            }
        }


        public async Task DeletePostAsync(string postId, string userId)
        {
            var existingPost = await _dynamoDbContext.LoadAsync<Post>(postId);

            if (existingPost == null) throw new Exception("Post not found");

            if (existingPost.UserId != userId)
            {
                throw new Exception("You can only delete your own posts");
            }

            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _s3bucketName,
                Key = postId
            };

            await _s3Client.DeleteObjectAsync(deleteRequest);

            await _dynamoDbContext.DeleteAsync<Post>(postId);
        }

        public async Task AddCommentAsync(string postId, CommentRequest request)
        {
            var post = await _dynamoDbContext.LoadAsync<Post>(postId);

            if (post == null) throw new Exception("Post not found");

            var user = await _dynamoDbContext.LoadAsync<User>(request.UserId);

            if (user == null) throw new Exception("User not found");

            var newComment = new Comment
            {
                UserId = request.UserId,
                FullName = user.FullName,
                Content = request.Comment
            };

            post.Comments?.Add(newComment);
            await _dynamoDbContext.SaveAsync(post);
        }

        public async Task EditCommentAsync(string postId, string commentId, EditCommentRequest request)
        {
            var post = await _dynamoDbContext.LoadAsync<Post>(postId);

            if (post == null) throw new Exception("Post not found");

            var comment = post.Comments?.FirstOrDefault(c => c.CommentId == commentId);

            if (comment == null) throw new Exception("Comment not found");

            comment.Content = request.Content;

            await _dynamoDbContext.SaveAsync(post);
        }

        public async Task DeleteCommentAsync(string postId, string commentId)
        {
            var post = await _dynamoDbContext.LoadAsync<Post>(postId);

            if (post == null) throw new Exception("Post not found");

            var comment = post.Comments?.FirstOrDefault(c => c.CommentId == commentId);

            if (comment == null) throw new Exception("Comment not found");

            post.Comments?.Remove(comment);

            await _dynamoDbContext.SaveAsync(post);
        }

        public async Task EditPostAsync(string postId, Post updatedPost)
        {
            try
            {
                var existingPost = await _dynamoDbContext.LoadAsync<Post>(postId);

                if (existingPost == null)
                {
                    throw new Exception($"Post with ID {postId} not found");
                }

                if (existingPost.UserId != updatedPost.UserId)
                {
                    throw new UnauthorizedAccessException("You can only edit your own posts");
                }

                existingPost.PostCaption = updatedPost.PostCaption;
                existingPost.Location = updatedPost.Location;
                existingPost.PostedDate = updatedPost.PostedDate;

                await _dynamoDbContext.SaveAsync(existingPost);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating post with ID {postId}: {ex.Message}", ex);
            }
        }

        public async Task<int> GetCommentsCount(string postId)
        {
            try
            {
                var post = await _dynamoDbContext.LoadAsync<Post>(postId);

                if (post == null) throw new Exception("Post not found");

                var commentCount = post.Comments?.Count ?? 0;

                return commentCount;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving post: {ex.Message}");
                return 0;
            }
        }
    }
}
