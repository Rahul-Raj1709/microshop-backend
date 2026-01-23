using AuthAPI.Models;
using AuthAPI.Repositories;
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
        if (await _userRepo.GetUserByEmail(request.Email) != null)
            return BadRequest("Email already exists.");

        // Generate OTP
        var otp = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

        var user = new User
        {
            Name = request.Name,
            Username = request.Username,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Role = "Customer",
            OtpCode = otp,
            OtpExpiry = DateTime.UtcNow.AddMinutes(10),
            IsActive = false,
            PasswordHash = "", // Will be set on OTP verification
            Preferences = new UserPreferences()
        };

        await _userRepo.CreateUser(user);

        // In a real app, send email here
        Console.WriteLine($"[EMAIL SENT] OTP for {request.Email}: {otp}");

        return Ok(new { Message = "User registered. Check email for OTP.", MockOtp = otp });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _userRepo.GetUserByEmail(request.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return BadRequest("Invalid email or password.");

        if (!user.IsActive)
            return BadRequest("Account is not active.");

        var token = CreateToken(user);
        var refreshToken = GenerateRefreshToken();

        await _userRepo.SaveRefreshToken(user.Id, refreshToken);

        return Ok(new AuthResponse(token, user.Role, user.Name, refreshToken));
    }

    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp(VerifyOtpRequest request)
    {
        var user = await _userRepo.GetUserByEmail(request.Email);
        if (user == null) return BadRequest("User not found.");

        if (user.OtpCode != request.Otp || user.OtpExpiry < DateTime.UtcNow)
            return BadRequest("Invalid or expired OTP.");

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _userRepo.ActivateUserAndSetPassword(user.Id, hashedPassword);

        return Ok("Account activated and password set.");
    }

    private string CreateToken(User user)
    {
        var claims = new List<Claim>
        {
            new Claim("username", user.Username),
            new Claim("email", user.Email),
            new Claim("role", user.Role),
            new Claim("userid", user.Id.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(60), // Short lived access token
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}