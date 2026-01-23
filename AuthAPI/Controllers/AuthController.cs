using AuthAPI.Models;
using AuthAPI.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OtpNet; // Required for 2FA
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LoginRequest = AuthAPI.Models.LoginRequest;
using RegisterRequest = AuthAPI.Models.RegisterRequest;

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

    // ==========================================
    // 1. PUBLIC AUTH (Register, Login, Verify)
    // ==========================================

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
            PasswordHash = "",
            Preferences = new UserPreferences() // Initialize JSONB
        };

        await _userRepo.CreateUser(user);

        // In production: SendEmail(user.Email, otp)
        Console.WriteLine($"[EMAIL SENT] OTP for {request.Email}: {otp}");

        return Ok(new { Message = "User registered. Check email for OTP.", MockOtp = otp });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _userRepo.GetUserByEmail(request.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return BadRequest("Invalid email or password.");

        if (!user.IsActive) return BadRequest("Account is not active.");

        // CHECK 2FA STATUS
        if (user.Preferences.TwoFactorEnabled)
        {
            return StatusCode(202, new { Message = "2FA Required", RequiresTwoFactor = true });
        }

        return GenerateAuthResponse(user);
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

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        var user = await _userRepo.GetUserByEmail(request.Email);
        if (user == null) return BadRequest("User not found.");

        string otp = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        await _userRepo.UpdateUserOtp(user.Id, otp, DateTime.UtcNow.AddMinutes(10));

        return Ok(new { Message = "OTP sent.", MockOtp = otp });
    }

    // ==========================================
    // 2. TWO-FACTOR AUTHENTICATION (2FA)
    // ==========================================

    [Authorize]
    [HttpPost("2fa/setup")]
    public async Task<IActionResult> SetupTwoFactor()
    {
        var userId = int.Parse(User.FindFirst("userid")?.Value ?? "0");
        var user = await _userRepo.GetUserById(userId);
        if (user == null) return NotFound();

        var key = KeyGeneration.GenerateRandomKey(20);
        var base32String = Base32Encoding.ToString(key);

        user.Preferences.TwoFactorSecret = base32String;
        await _userRepo.UpdateUserPreferences(userId, user.Preferences);

        var otpUri = $"otpauth://totp/MicroShop:{user.Email}?secret={base32String}&issuer=MicroShop";
        return Ok(new TwoFactorSetupResponse(otpUri, base32String));
    }

    [Authorize]
    [HttpPost("2fa/enable")]
    public async Task<IActionResult> EnableTwoFactor([FromBody] TwoFactorVerifyRequest request)
    {
        var userId = int.Parse(User.FindFirst("userid")?.Value ?? "0");
        var user = await _userRepo.GetUserById(userId);

        if (user == null || string.IsNullOrEmpty(user.Preferences.TwoFactorSecret))
            return BadRequest("Setup not initiated.");

        var secretBytes = Base32Encoding.ToBytes(user.Preferences.TwoFactorSecret);
        var totp = new Totp(secretBytes);

        if (totp.VerifyTotp(request.Code, out _, new VerificationWindow(2, 2)))
        {
            user.Preferences.TwoFactorEnabled = true;
            await _userRepo.UpdateUserPreferences(userId, user.Preferences);
            return Ok(new { Message = "2FA Enabled" });
        }
        return BadRequest("Invalid code.");
    }

    [Authorize]
    [HttpPost("2fa/disable")]
    public async Task<IActionResult> DisableTwoFactor()
    {
        var userId = int.Parse(User.FindFirst("userid")?.Value ?? "0");
        var user = await _userRepo.GetUserById(userId);

        user.Preferences.TwoFactorEnabled = false;
        user.Preferences.TwoFactorSecret = null;
        await _userRepo.UpdateUserPreferences(userId, user.Preferences);

        return Ok(new { Message = "2FA Disabled" });
    }

    [HttpPost("login-2fa")]
    public async Task<IActionResult> Login2Fa([FromBody] TwoFactorLoginRequest request)
    {
        var user = await _userRepo.GetUserByEmail(request.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return BadRequest("Invalid credentials.");

        if (!user.Preferences.TwoFactorEnabled || string.IsNullOrEmpty(user.Preferences.TwoFactorSecret))
            return BadRequest("2FA not enabled.");

        var secretBytes = Base32Encoding.ToBytes(user.Preferences.TwoFactorSecret);
        var totp = new Totp(secretBytes);

        if (!totp.VerifyTotp(request.Code, out _, new VerificationWindow(2, 2)))
            return BadRequest("Invalid Code.");

        return GenerateAuthResponse(user);
    }

    // ==========================================
    // 3. SUPER ADMIN MANAGEMENT
    // ==========================================

    [HttpPost("register-admin")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> RegisterAdmin(RegisterRequest request)
    {
        if (await _userRepo.GetUserByEmail(request.Email) != null)
            return BadRequest("Email already exists.");

        // Admins created by SuperAdmin are active immediately if password is provided
        var user = new User
        {
            Name = request.Name,
            Username = request.Username,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Role = "Admin",
            IsActive = true,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Preferences = new UserPreferences()
        };

        await _userRepo.CreateUser(user);
        return Ok(new { Message = $"Admin {request.Username} created." });
    }

    [HttpGet("admins")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetAllAdmins()
    {
        // Requires a method in Repo: GetUsersByRole(string role)
        // If not present, you might need to add it to IUserRepository
        var users = await _userRepo.GetUsersByRole("Admin");

        var adminDtos = users.Select(u => new AdminDto(
            u.Id, u.Name, u.Username, u.Email, u.PhoneNumber, u.IsActive
        ));

        return Ok(adminDtos);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateAdminRequest request)
    {
        var user = await _userRepo.GetUserById(id);
        if (user == null) return NotFound();

        // Only update specific fields
        user.Name = request.Name;
        user.Email = request.Email;
        user.PhoneNumber = request.PhoneNumber;
        user.IsActive = request.IsActive;

        // Use the generic UpdateUser method from repo
        await _userRepo.UpdateUser(user);

        // If you have a specific method to update Active status, call it here too
        // For now assuming UpdateUser handles these fields

        return Ok("User updated successfully.");
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        // Remove the second argument "0", just pass the ID
        await _userRepo.DeleteUser(id);
        return Ok("User deleted.");
    }
    
    // ==========================================
    // 4. HELPERS
    // ==========================================

    private IActionResult GenerateAuthResponse(User user)
    {
        var token = CreateToken(user);
        var refreshToken = GenerateRefreshToken();
        // Fire and forget save token
        _userRepo.SaveRefreshToken(user.Id, refreshToken).Wait();

        return Ok(new AuthResponse(token, user.Role, user.Name, refreshToken));
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
        if (!string.IsNullOrEmpty(user.Name)) claims.Add(new Claim("name", user.Name));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
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