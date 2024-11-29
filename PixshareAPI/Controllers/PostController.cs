using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PixshareAPI.Interface;
using PixshareAPI.Models;

namespace PixshareAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PostController : ControllerBase
    {
        private readonly IPostRepository _postRepository;

        public PostController(IPostRepository postRepository)
        {
            _postRepository = postRepository;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreatePost([FromForm] Post post, [FromForm] IFormFile movieFile)
        {
            try
            {
                await _postRepository.CreatePostAsync(post, movieFile);
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpGet("get-all")]
        public async Task<IActionResult> GetAllPosts()
        {
            try
            {
                var posts = await _postRepository.GetAllPostsAsync();
                return Ok(posts);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpGet("get-by-id/{postId}")]
        public async Task<IActionResult> GetPostById(string postId)
        {
            try
            {
                var post = await _postRepository.GetPostByIdAsync(postId);
                if (post == null) return NotFound();
                return Ok(post);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPost("add-comment/{postId}")]
        public async Task<IActionResult> AddComment(string postId, [FromBody] CommentRequest request)
        {
            try
            {
                await _postRepository.AddCommentAsync(postId, request);

                var updatedPost = await _postRepository.GetPostByIdAsync(postId);
                return Ok(updatedPost);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPut("edit-comment/{postId}/{commentId}")]
        public async Task<IActionResult> EditComment(string postId, string commentId, [FromBody] EditCommentRequest request)
        {
            try
            {
                await _postRepository.EditCommentAsync(postId, commentId, request);

                var updatedPost = await _postRepository.GetPostByIdAsync(postId);
                return Ok(updatedPost);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpDelete("delete-comment/{postId}/{commentId}")]
        public async Task<IActionResult> DeleteComment(string postId, string commentId)
        {
            try
            {
                await _postRepository.DeleteCommentAsync(postId, commentId);

                var updatedPost = await _postRepository.GetPostByIdAsync(postId);
                return Ok(updatedPost);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpGet("get-all-by-user/{userId}")]
        public async Task<IActionResult> GetAllPostsByUser(string userId)
        {
            try
            {
                var posts = await _postRepository.GetPostsByUserIdAsync(userId);

                return Ok(posts);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }


        [HttpPut("edit/{postId}")]
        public async Task<IActionResult> EditPost(string postId, [FromBody] Post updatedPost)
        {
            try
            {
                await _postRepository.EditPostAsync(postId, updatedPost);

                return Ok(updatedPost);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
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
                await _postRepository.DeletePostAsync(postId, userId);

                var posts = await _postRepository.GetPostsByUserIdAsync(userId);

                return Ok(posts);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
    }

}
