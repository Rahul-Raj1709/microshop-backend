using AuthAPI.Models;
using AuthAPI.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserRepository _userRepo;

    public UserController(IUserRepository userRepo)
    {
        _userRepo = userRepo;
    }

    // GET: api/user/profile
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = int.Parse(User.FindFirst("userid")?.Value ?? "0");
        var user = await _userRepo.GetUserById(userId);

        if (user == null) return NotFound("User not found");

        var response = new UserProfileResponse(
            user.Id,
            user.Name,
            user.Username,
            user.Email,
            user.PhoneNumber,
            user.Role,
            user.AvatarUrl,
            user.Preferences,
            user.Addresses // Now a list from the table
        );

        return Ok(response);
    }

    // PUT: api/user/profile (Updates basic info)
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = int.Parse(User.FindFirst("userid")?.Value ?? "0");
        var user = await _userRepo.GetUserById(userId);
        if (user == null) return NotFound();

        user.Name = request.Name;
        user.PhoneNumber = request.PhoneNumber;
        user.AvatarUrl = request.AvatarUrl;

        await _userRepo.UpdateUser(user);
        return Ok(new { Message = "Profile updated successfully" });
    }

    // POST: api/user/addresses
    [HttpPost("addresses")]
    public async Task<IActionResult> AddAddress([FromBody] AddAddressRequest request)
    {
        var userId = int.Parse(User.FindFirst("userid")?.Value ?? "0");

        var address = new UserAddress
        {
            UserId = userId,
            Label = request.Label,
            AddressLine = request.AddressLine,
            IsDefault = request.IsDefault
        };

        await _userRepo.AddAddress(address);
        return Ok(new { Message = "Address added" });
    }

    // DELETE: api/user/addresses/{id}
    [HttpDelete("addresses/{id}")]
    public async Task<IActionResult> DeleteAddress(int id)
    {
        var userId = int.Parse(User.FindFirst("userid")?.Value ?? "0");
        await _userRepo.DeleteAddress(id, userId);
        return Ok(new { Message = "Address deleted" });
    }

    // PUT: api/user/preferences
    [HttpPut("preferences")]
    public async Task<IActionResult> UpdatePreferences([FromBody] UserPreferences prefs)
    {
        var userId = int.Parse(User.FindFirst("userid")?.Value ?? "0");
        await _userRepo.UpdateUserPreferences(userId, prefs);
        return Ok(new { Message = "Preferences updated" });
    }

    // PUT: api/user/change-password
    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = int.Parse(User.FindFirst("userid")?.Value ?? "0");

        var user = await _userRepo.GetUserById(userId);
        if (user == null) return NotFound("User not found");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return BadRequest("Incorrect current password.");
        }

        var newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _userRepo.UpdatePassword(userId, newHash);

        return Ok(new { Message = "Password updated successfully" });
    }
}