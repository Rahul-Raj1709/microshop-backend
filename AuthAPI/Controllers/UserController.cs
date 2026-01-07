using AuthAPI.Models;
using AuthAPI.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize] // All endpoints require login
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

        // Map to DTO to hide PasswordHash, OtpCode, etc.
        var response = new UserProfileResponse(
            user.Id,
            user.Name,
            user.Username,
            user.Email,
            user.PhoneNumber,
            user.Role,
            user.AvatarUrl,
            user.Preferences,
            user.ProfileData
        );

        return Ok(response);
    }
    // PUT: api/user/profile
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = int.Parse(User.FindFirst("userid")?.Value ?? "0");

        await _userRepo.UpdateUserProfile(
            userId,
            request.Name,
            request.PhoneNumber,
            request.AvatarUrl,
            request.ProfileData
        );

        return Ok(new { Message = "Profile updated successfully" });
    }

    // PUT: api/user/preferences
    [HttpPut("preferences")]
    public async Task<IActionResult> UpdatePreferences([FromBody] UpdatePreferencesRequest request)
    {
        var userId = int.Parse(User.FindFirst("userid")?.Value ?? "0");

        await _userRepo.UpdateUserPreferences(userId, request.Preferences);

        return Ok(new { Message = "Preferences saved successfully" });
    }
}