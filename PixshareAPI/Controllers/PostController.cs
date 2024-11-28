using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.S3;
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
                string.IsNullOrEmpty(post.Location) ||
                string.IsNullOrEmpty(post.S3Url))
            {
                return BadRequest("Please enter your information!");
            }

            if (movieFile != null && movieFile.Length > 0)
            {
                var newPostId = Guid.NewGuid().ToString();

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
                catch (Exception ex)
                {
                    Console.WriteLine("Error uploading file: " + ex.Message);
                    return BadRequest("File upload failed");
                }
            }
            else
            {
                return BadRequest("No file selected");
            }

            try
            {
                await _dynamoDbContext.SaveAsync(post);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error saving to database: {ex.Message}");
            }

            return Ok();
        }
    }
}
