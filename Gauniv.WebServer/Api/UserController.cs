using Gauniv.WebServer.Data;
using Gauniv.WebServer.Dtos.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Gauniv.WebServer.Api
{
    [Route("user")]
    [ApiController]
    public class UserController(
        UserManager<User> userManager,
        SignInManager<User> signInManager) : ControllerBase
    {

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var result = await userManager.ChangePasswordAsync(user, changePasswordDto.CurrentPassword,
                changePasswordDto.NewPassword);
            if (!result.Succeeded)
            {
                return BadRequest(
                    "Password change failed. Please ensure your current password is correct and the new password meets the requirements.");
            }

            return Ok("Password changed successfully.");

        }


        [HttpPost("change-email")]
        public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailDto changeEmailDto)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            var passwordCheck = await userManager.CheckPasswordAsync(user, changeEmailDto.CurrentPassword);
            if (!passwordCheck)
            {
                return BadRequest("Current password is incorrect.");
            }

            var newEmail = changeEmailDto.NewEmail;
            var newUsername = newEmail; 
            
            
            var usernameResult = await userManager.SetUserNameAsync(user, newUsername);

            if (!usernameResult.Succeeded)
            {
                return BadRequest("Username change failed. Please ensure the new username is valid and not already in use.");
            }
            
            var emailToken = await userManager.GenerateChangeEmailTokenAsync(user, newEmail);
            var emailResult = await userManager.ChangeEmailAsync(user, newEmail, emailToken);
            
            if (!emailResult.Succeeded)
            {
                return BadRequest("Email change failed. Please ensure the new email is valid and not already in use.");
            }
            
            return Ok("Email changed successfully.");

        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            // Signs the user out (clears cookies / authentication session)
            await signInManager.SignOutAsync();
            return Ok("Logged out successfully.");
        }
    }
}
