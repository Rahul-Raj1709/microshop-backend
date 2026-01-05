namespace AuthAPI.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string OtpCode { get; set; } = string.Empty;
    public DateTime? OtpExpiry { get; set; }
    public bool IsActive { get; set; }
}

// DTOs for the new flows
public record RegisterRequest(string Username, string Email, string PhoneNumber);
public record VerifyOtpRequest(string Email, string Otp, string NewPassword);
public record LoginRequest(string Email, string Password);
public record ForgotPasswordRequest(string Email);