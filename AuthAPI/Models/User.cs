namespace AuthAPI.Models;

// 1. Internal Entity (Maps to DB table)
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // Added
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string OtpCode { get; set; } = string.Empty;
    public DateTime? OtpExpiry { get; set; }
    public bool IsActive { get; set; }
}

// 2. Response DTOs (What we send TO the client)
public record AdminDto(
    int Id,
    string Name,      // Added
    string Username,
    string Email,
    string PhoneNumber,
    bool IsActive
);

public record AuthResponse(string Token, string Role, string Name); // Added Name to login response

// 3. Request DTOs (What we receive FROM the client)

// Registration (Used for both Customer and Admin creation)
public record RegisterRequest(
    string Name,      // Added
    string Username,
    string Email,
    string PhoneNumber,
    string? Password = null // Optional (Admin only)
);

// Update (Restricted fields only)
public record UpdateAdminRequest(
    string Name,      // Added
    string Email,
    string PhoneNumber,
    bool IsActive
);

// Login / Security
public record LoginRequest(string Email, string Password);
public record VerifyOtpRequest(string Email, string Otp, string NewPassword);
public record ForgotPasswordRequest(string Email);