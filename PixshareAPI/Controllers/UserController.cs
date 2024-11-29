using System.Net.Mail;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using PixshareAPI.Interface;
using PixshareAPI.Models;

namespace PixshareAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _userRepository;

        public UserController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            if (string.IsNullOrEmpty(user.FullName) ||
                string.IsNullOrEmpty(user.EmailAddress) ||
                string.IsNullOrEmpty(user.Username) ||
                string.IsNullOrEmpty(user.UserPassword))
            {
                return BadRequest("Please enter your information!");
            }

            var existingUser = await _userRepository.GetUserByUsernameAsync(user.Username);

            if (existingUser != null)
            {
                return BadRequest("Username already exists.");
            }

            await _userRepository.SaveUserAsync(user);

            return Ok(new { Message = "User registered successfully!" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] User user)
        {
            if (string.IsNullOrEmpty(user.EmailAddress) ||
                string.IsNullOrEmpty(user.UserPassword))
            {
                return BadRequest("Please enter your credentials!");
            }

            var foundUser = await _userRepository.GetUserByEmailAsync(user.EmailAddress);

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
            });
        }

        [HttpPatch("update-password/{userId}")]
        public async Task<IActionResult> UpdatePassword(string userId, [FromBody] PasswordRequest request)
        {
            if (string.IsNullOrEmpty(request.newPassword))
            {
                return BadRequest("Password is required.");
            }

            var user = await _userRepository.GetUserByIdAsync(userId);

            if (user == null)
            {
                return NotFound($"User with ID {userId} not found.");
            }

            try
            {
                user.UserPassword = request.newPassword;
                await _userRepository.UpdateUserAsync(user);

                return Ok(new { Message = "Password updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error updating password: {ex.Message}");
            }
        }

        [HttpDelete("delete/{userId}")]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            await _userRepository.DeleteUserAsync(userId);

            return NoContent();
        }
    }

}
