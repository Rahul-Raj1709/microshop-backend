using AuthAPI.Models;
using AuthAPI.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography; // Required for RandomNumberGenerator
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

    // 1. REGISTER (Customer)
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        return await RegisterUserInternal(request, "Customer");
    }

    // 2. REGISTER ADMIN (Fixed Hashing)
    [HttpPost("register-admin")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> RegisterAdmin(RegisterRequest request)
    {
        if (await _userRepo.GetUserByEmail(request.Email) != null)
            return BadRequest("Email already exists.");

        if (!string.IsNullOrEmpty(request.Password))
        {
            var user = new User
            {
                Name = request.Name,
                Username = request.Username,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                Role = "Admin",
                IsActive = true,
                // SECURITY FIX: Use BCrypt for hashing
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                OtpCode = null,
                OtpExpiry = null
            };

            await _userRepo.CreateUser(user);
            return Ok(new { Message = $"Admin {request.Username} created and activated." });
        }

        return await RegisterUserInternal(request, "Admin");
    }

    // 3. LOGIN (Fixed Verification)
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _userRepo.GetUserByEmail(request.Email);

        // SECURITY FIX: Use BCrypt.Verify
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return BadRequest("Invalid email or password.");

        if (!user.IsActive)
            return BadRequest("Account is not active.");

        string token = CreateToken(user);
        return Ok(new AuthResponse(token, user.Role, user.Name));
    }

    // 4. GET ALL ADMINS
    [HttpGet("admins")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetAllAdmins()
    {
        var users = await _userRepo.GetUsersByRole("Admin");

        // Map to AdminDto (Safe model)
        var adminDtos = users.Select(u => new AdminDto(
            u.Id,
            u.Name, // Added
            u.Username,
            u.Email,
            u.PhoneNumber,
            u.IsActive
        ));

        return Ok(adminDtos);
    }

    // 5. UPDATE USER
    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateAdminRequest request)
    {
        // Map UpdateRequest -> Entity
        var userToUpdate = new User
        {
            Id = id,
            Name = request.Name, // Added
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            IsActive = request.IsActive
        };

        await _userRepo.UpdateUser(userToUpdate);
        return Ok("User updated successfully.");
    }

    // (Internal Helper: Updated to include Name)
    private async Task<IActionResult> RegisterUserInternal(RegisterRequest request, string role)
    {
        if (await _userRepo.GetUserByEmail(request.Email) != null)
            return BadRequest("Email already exists.");

        string otp = GenerateOtp();

        var user = new User
        {
            Name = request.Name, // Added
            Username = request.Username,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Role = role,
            OtpCode = otp,
            OtpExpiry = DateTime.UtcNow.AddMinutes(10),
            IsActive = false,
            PasswordHash = ""
        };

        await _userRepo.CreateUser(user);
        Console.WriteLine($"[EMAIL SENT] OTP: {otp}");
        return Ok(new { Message = $"User registered as {role}. Check email for OTP.", MockOtp = otp });
    }

    // 6. VERIFY OTP (Fixed Hashing)
    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp(VerifyOtpRequest request)
    {
        var user = await _userRepo.GetUserByEmail(request.Email);
        if (user == null) return BadRequest("User not found.");

        // Ensure secure comparison (optional but good practice) and expiry check
        if (user.OtpCode != request.Otp || user.OtpExpiry < DateTime.UtcNow)
            return BadRequest("Invalid OTP.");

        // SECURITY FIX: Hash the new password with BCrypt before saving
        string newPasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

        await _userRepo.ActivateUserAndSetPassword(user.Id, newPasswordHash);
        return Ok("Password set successfully.");
    }

    // 7. DELETE USER
    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        await _userRepo.DeleteUser(id);
        return Ok("User deleted successfully.");
    }

    private string GenerateOtp()
    {
        // SECURITY FIX: Use Cryptographically Secure RNG
        return RandomNumberGenerator.GetInt32(100000, 999999).ToString();
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
    
    // 8. FORGOT PASSWORD
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        var user = await _userRepo.GetUserByEmail(request.Email);
        if (user == null) return BadRequest("User not found.");
        string otp = GenerateOtp();
        await _userRepo.UpdateUserOtp(user.Id, otp, DateTime.UtcNow.AddMinutes(10));
        Console.WriteLine($"[EMAIL SENT] OTP: {otp}");
        return Ok(new { Message = "OTP sent.", MockOtp = otp });
    }
}