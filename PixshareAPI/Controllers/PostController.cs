using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PixshareAPI.Models;

namespace PixshareAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PostController : ControllerBase
    {
        private readonly IAmazonS3 _s3Client;
        private readonly IDynamoDBContext _dynamoDbContext;
        private const string _s3bucketName = "pixshare-api";
        private readonly IAmazonDynamoDB _dynamoDbClient;

        public PostController(IAmazonS3 s3Client, IAmazonDynamoDB dynamoDbClient, IDynamoDBContext dbContext)
        {
            _s3Client = s3Client;
            _dynamoDbContext = dbContext;
            _dynamoDbClient = dynamoDbClient;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreatePost([FromForm] Post post, [FromForm] IFormFile movieFile)
        {
            if (string.IsNullOrEmpty(post.UserId) ||
                string.IsNullOrEmpty(post.PostCaption) ||
                string.IsNullOrEmpty(post.Location))
            {
                return BadRequest("Required fields are missing!");
            }

            if (movieFile == null || movieFile.Length == 0)
            {
                return BadRequest("No file selected or file is empty.");
            }

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
                Console.WriteLine("Error uploading file: " + ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error uploading file to S3.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error uploading file: " + ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while uploading the file.");
            }

            try
            {
                post.PostedDate = DateTime.UtcNow;
                await _dynamoDbContext.SaveAsync(post);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DynamoDB Error: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error saving to database: {ex.Message}");
            }

            return Ok();
        }

        [HttpGet("get-all")]
        public async Task<IActionResult> GetAllPosts()
        {
            try
            {
                var posts = await _dynamoDbContext.ScanAsync<Post>(new List<ScanCondition>()).GetRemainingAsync();

                var enrichedPosts = new List<object>();

                foreach (var post in posts)
                {
                    var user = await _dynamoDbContext.LoadAsync<User>(post.UserId);

                    enrichedPosts.Add(new
                    {
                        post.PostId,
                        post.UserId,
                        post.PostCaption,
                        post.Location,
                        post.PostedDate,
                        post.S3Url,
                        post.Comments,
                        FullName = user?.FullName ?? "Unknown User",
                        Username = user?.Username ?? "Unknown User"
                    });
                }

                return Ok(enrichedPosts);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error retrieving posts: {ex.Message}");
            }
        }

        [HttpGet("get-by-id/{postId}")]
        public async Task<IActionResult> GetPostById(string postId)
        {
            try
            {
                var post = await _dynamoDbContext.LoadAsync<Post>(postId);

                if (post == null)
                {
                    return NotFound("Post not found");
                }

                var user = await _dynamoDbContext.LoadAsync<User>(post.UserId);

                var enrichedPosts = new
                {
                    post.PostId,
                    post.UserId,
                    post.PostCaption,
                    post.Location,
                    post.PostedDate,
                    post.S3Url,
                    post.Comments,
                    FullName = user?.FullName ?? "Unknown User",
                    Username = user?.Username ?? "Unknown User"
                };

                return Ok(enrichedPosts);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error retrieving post: {ex.Message}");
            }
        }

        [HttpPost("add-comment/{postId}")]
        public async Task<IActionResult> AddComment(string postId, [FromBody] CommentRequest request)
        {
            var post = await _dynamoDbContext.LoadAsync<Post>(postId);

            if (post == null) return NotFound("Post not found");

            var user = await _dynamoDbContext.LoadAsync<User>(request.UserId);

            if (user == null) return NotFound("User not found");

            var existingComment = post.Comments?.FirstOrDefault(p => p.UserId == request.UserId);

            if (existingComment != null)
            {
                existingComment.Content = request.Comment;
            }
            else
            {
                var newComment = new Comment
                {
                    UserId = request.UserId,
                    FullName = user.FullName,
                    Content = request.Comment
                };

                post?.Comments?.Add(newComment);
            }

            await _dynamoDbContext.SaveAsync(post);

            return Ok(post);
        }

        [HttpPut("edit-comment/{postId}/{commentId}")]
        public async Task<IActionResult> EditComment(string postId, string commentId, [FromBody] EditCommentRequest request)
        {
            try
            {
                var post = await _dynamoDbContext.LoadAsync<Post>(postId);

                if (post == null)
                {
                    return NotFound("Post not found");
                }

                var comment = post.Comments?.FirstOrDefault(c => c.CommentId == commentId);

                if (comment == null)
                {
                    return NotFound("Comment not found");
                }

                if (comment.UserId != request.UserId)
                {
                    return Unauthorized("You cannot edit this comment");
                }

                comment.Content = request.Content;

                await _dynamoDbContext.SaveAsync(post);

                return Ok(post);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error updating comment: {ex.Message}");
            }
        }

        [HttpDelete("delete-comment/{postId}/{commentId}")]
        public async Task<IActionResult> DeleteComment(string postId, string commentId)
        {
            try
            {
                var post = await _dynamoDbContext.LoadAsync<Post>(postId);

                if (post == null)
                {
                    return NotFound("Post not found");
                }

                var comment = post.Comments?.FirstOrDefault(c => c.CommentId == commentId);

                if (comment == null)
                {
                    return NotFound("Comment not found");
                }

                _ = post.Comments?.Remove(comment);

                await _dynamoDbContext.SaveAsync(post);

                return Ok(post);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error deleting comment: {ex.Message}");
            }
        }

        [HttpGet("get-all-by-user/{userId}")]
        public async Task<IActionResult> GetAllPostsByUser(string userId)
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

                    enrichedPosts.Add(new
                    {
                        post.PostId,
                        post.UserId,
                        post.PostCaption,
                        post.Location,
                        post.PostedDate,
                        post.S3Url,
                        post.Comments,
                        FullName = user?.FullName ?? "Unknown User",
                        Username = user?.Username ?? "Unknown User"
                    });
                }

                return Ok(enrichedPosts);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error retrieving posts: {ex.Message}");
            }
        }

        [HttpPut("edit/{postId}")]
        public async Task<IActionResult> EditPost(string postId, [FromBody] Post updatedPost)
        {
            try
            {
                var existingPost = await _dynamoDbContext.LoadAsync<Post>(postId);

                if (existingPost == null)
                {
                    return NotFound("Post not found");
                }

                if (existingPost.UserId != updatedPost.UserId)
                {
                    return Unauthorized("You can only edit your own posts");
                }

                existingPost.PostCaption = updatedPost.PostCaption;
                existingPost.Location = updatedPost.Location;
                existingPost.PostedDate = updatedPost.PostedDate;

                await _dynamoDbContext.SaveAsync(existingPost);

                return Ok(existingPost);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error editing post: {ex.Message}");
            }
        }

        [HttpDelete("delete/{postId}")]
        public async Task<IActionResult> DeletePost(string postId, [FromQuery] string userId)
        {
            try
            {
                var existingPost = await _dynamoDbContext.LoadAsync<Post>(postId);

                if (existingPost == null)
                {
                    return NotFound("Post not found");
                }

                if (existingPost.UserId != userId)
                {
                    return Unauthorized("You can only delete your own posts");
                }

                var deleteRequest = new DeleteObjectRequest
                {
                    BucketName = _s3bucketName,
                    Key = postId
                };

                await _s3Client.DeleteObjectAsync(deleteRequest);

                await _dynamoDbContext.DeleteAsync<Post>(postId);

                return Ok("Post deleted successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error deleting post: {ex.Message}");
            }
        }



    }
}
