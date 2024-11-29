using System.Net.Mail;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using PixshareAPI.Models;

namespace PixshareAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController(IDynamoDBContext dbContext) : ControllerBase
    {
        private readonly IDynamoDBContext _dynamoDbContext = dbContext;

        // Registration Endpoint
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            if (user.FullName == "" || user.EmailAddress == "" || user.Username == "" || user.UserPassword == "")
            {
                return BadRequest("Please enter your information!");
            }

            var existingUser = await _dynamoDbContext.LoadAsync<User>(user.Username);

            if (existingUser != null)
            {
                return BadRequest("Username already exists.");
            }

            await _dynamoDbContext.SaveAsync(user);

            return Ok();
        }

        // Login Endpoint
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] User user)
        {
            if (user.EmailAddress == "" || user.UserPassword == "")
            {
                return BadRequest("Please enter your credentials!");
            }

            var logUser = await _dynamoDbContext
                .ScanAsync<User>(new List<ScanCondition>
                {
                    new ScanCondition("EmailAddress", Amazon.DynamoDBv2.DocumentModel.ScanOperator.Equal, user.EmailAddress)
                }).GetRemainingAsync();

            var foundUser = logUser.FirstOrDefault();

            if (foundUser == null)
            {
                return NotFound("User not found.");
            }

            if (foundUser.UserPassword != user.UserPassword)
            {
                return Unauthorized("Invalid username or password.");
            }

            return Ok(new
                {
                    Message = "Login successful",
                    UserId = foundUser.UserId,
                    Username = foundUser.Username,
                    EmailAddress = foundUser.EmailAddress,
                    FullName = foundUser.FullName
                }
            );
        }

        [HttpPatch("update-password/{userId}")]
        public async Task<IActionResult> UpdatePassword(string userId, [FromBody] PasswordRequest request)
        {
            if (string.IsNullOrEmpty(request.newPassword))
            {
                return BadRequest("Password is required.");
            }

            var user = await _dynamoDbContext.LoadAsync<User>(userId);

            if (user == null)
            {
                return NotFound($"User with ID {userId} not found.");
            }

            try
            {
                user.UserPassword = request.newPassword;

                await _dynamoDbContext.SaveAsync(user);

                return Ok(new { Message = "Password updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error updating password: {ex.Message}");
            }
        }



    }
}
