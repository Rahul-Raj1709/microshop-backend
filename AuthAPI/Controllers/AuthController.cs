using AuthAPI.Models;
using AuthAPI.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AuthAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepo;
    private readonly IConfiguration _config;

    public AuthController(IUserRepository userRepo, IConfiguration config)
    {
        _userRepo = userRepo;
        _config = config;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        return await RegisterUserInternal(request, "Customer");
    }

    [HttpPost("register-admin")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> RegisterAdmin(RegisterRequest request)
    {
        return await RegisterUserInternal(request, "Admin");
    }

    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp(VerifyOtpRequest request)
    {
        var user = await _userRepo.GetUserByEmail(request.Email);
        if (user == null) return BadRequest("User not found.");

        if (user.OtpCode != request.Otp || user.OtpExpiry < DateTime.UtcNow)
            return BadRequest("Invalid or Expired OTP.");

        // Hash new password and activate user
        string hash = HashPassword(request.NewPassword);
        await _userRepo.ActivateUserAndSetPassword(user.Id, hash);

        return Ok("Password set successfully. You can now login.");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _userRepo.GetUserByEmail(request.Email);

        if (user == null || user.PasswordHash != HashPassword(request.Password))
            return BadRequest("Invalid email or password.");

        if (!user.IsActive)
            return BadRequest("Account is not active. Please verify OTP.");

        string token = CreateToken(user);
        return Ok(new { Token = token, Role = user.Role });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        var user = await _userRepo.GetUserByEmail(request.Email);
        if (user == null) return BadRequest("User not found.");

        string otp = GenerateOtp();
        await _userRepo.UpdateUserOtp(user.Id, otp, DateTime.UtcNow.AddMinutes(10));

        // MOCK EMAIL SENDING
        Console.WriteLine($"[EMAIL SENT] To: {request.Email}, OTP: {otp}");

        return Ok(new { Message = "OTP sent to email.", MockOtp = otp });
    }

    [HttpGet("admins")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetAllAdmins()
    {
        var admins = await _userRepo.GetUsersByRole("Admin");
        return Ok(admins);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        // Optional: Prevent deleting yourself or SuperAdmins if needed
        await _userRepo.DeleteUser(id);
        return Ok("User deleted successfully.");
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] User user)
    {
        user.Id = id; // Ensure ID matches URL
        await _userRepo.UpdateUser(user);
        return Ok("User updated successfully.");
    }

    // --- HELPER METHODS ---

    private async Task<IActionResult> RegisterUserInternal(RegisterRequest request, string role)
    {
        if (await _userRepo.GetUserByEmail(request.Email) != null)
            return BadRequest("Email already exists.");

        string otp = GenerateOtp();

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Role = role,
            OtpCode = otp,
            OtpExpiry = DateTime.UtcNow.AddMinutes(10)
        };

        await _userRepo.CreateUser(user);

        // In production, use an Email Service here (SMTP/SendGrid)
        Console.WriteLine($"[EMAIL SENT] Welcome {role}! To: {request.Email}, OTP to set password: {otp}");

        return Ok(new { Message = $"User registered as {role}. Check email for OTP.", MockOtp = otp });
    }

    private string GenerateOtp() => new Random().Next(100000, 999999).ToString();

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    private string CreateToken(User user)
    {
        var claims = new List<Claim>
        {
            // USE CUSTOM SHORT NAMES:
            new Claim("username", user.Username),
            new Claim("email", user.Email),
            new Claim("role", user.Role),
            new Claim("userid", user.Id.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddDays(1),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}