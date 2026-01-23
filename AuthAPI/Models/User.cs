using System.Text.Json.Serialization;

namespace AuthAPI.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "Customer";
    public string Name { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string OtpCode { get; set; } = string.Empty;
    public DateTime? OtpExpiry { get; set; }
    public string RefreshToken { get; set; } = string.Empty; // New Column
    public UserPreferences Preferences { get; set; } = new();
    [JsonIgnore]
    public List<UserAddress> Addresses { get; set; } = new();
}

public class UpdateAdminRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

// Matches 'user_addresses' table
public class UserAddress
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Label { get; set; } = "Home"; // e.g., Home, Work
    public string AddressLine { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}

// Stored as JSONB
public class UserPreferences
{
    public string Theme { get; set; } = "system";
    public string Language { get; set; } = "en";
    public bool TwoFactorEnabled { get; set; }
    public string? TwoFactorSecret { get; set; }
    public NotificationPrefs Notifications { get; set; } = new();
    public SellerOpsPreferences? SellerOps { get; set; }
    public PlatformConfigPreferences? PlatformConfig { get; set; }
}

public class SellerOpsPreferences
{
    public bool AutoAcceptOrders { get; set; } = true;
    public string Currency { get; set; } = "USD";
}

public class PlatformConfigPreferences
{
    public bool MaintenanceMode { get; set; }
    public bool AllowRegistrations { get; set; } = true;
    public decimal CommissionRate { get; set; } = 5;
}
public class NotificationPrefs
{
    public bool Email { get; set; } = true;
    public bool Push { get; set; } = false;
    public bool Marketing { get; set; } = true;
}

// DTOs
public record RegisterRequest(string Name, string Username, string Email, string PhoneNumber, string? Password = null);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token, string Role, string Name, string RefreshToken);
public record UserProfileResponse(
    int Id,
    string Name,
    string Username,
    string Email,
    string PhoneNumber,
    string Role,
    string AvatarUrl,
    UserPreferences Preferences,
    List<UserAddress> Addresses
);

public record UpdateProfileRequest(string Name, string PhoneNumber, string AvatarUrl);
public record AddAddressRequest(string Label, string AddressLine, bool IsDefault);
public record VerifyOtpRequest(string Email, string Otp, string NewPassword);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record TwoFactorSetupResponse(string QrCodeUri, string ManualEntryKey);
public record TwoFactorVerifyRequest(string Code);
public record TwoFactorLoginRequest(string Email, string Code, string Password);
public record AdminDto(int Id, string Name, string Username, string Email, string PhoneNumber, bool IsActive);