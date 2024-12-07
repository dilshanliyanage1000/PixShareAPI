using Microsoft.AspNetCore.Mvc;
using PixshareAPI.Interface;
using PixshareAPI.Models;
using PixshareAPI.Repository;

namespace PixshareAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LikeController : ControllerBase
    {
        private readonly ILikeRepository _likeRepository;
        private readonly IPostRepository _postRepository;

        public LikeController(ILikeRepository likeRepository, IPostRepository postRepository)
        {
            _likeRepository = likeRepository;
            _postRepository = postRepository;
        }

        [HttpPost]
        public async Task<IActionResult> AddLike([FromBody] LikeRequest request)
        {
            try
            {
                await _likeRepository.AddLike(request.PostId, request.UserId);

                var updatedPost = await _postRepository.GetPostByIdAsync(request.PostId);

                return Ok(updatedPost);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpDelete]
        public async Task<IActionResult> RemoveLike([FromBody] LikeRequest request)
        {
            try
            {
                await _likeRepository.RemoveLike(request.PostId, request.UserId);

                var updatedPost = await _postRepository.GetPostByIdAsync(request.PostId);

                return Ok(updatedPost);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
    }
}
