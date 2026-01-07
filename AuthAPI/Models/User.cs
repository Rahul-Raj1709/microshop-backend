namespace AuthAPI.Models;

// 1. Entity
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string OtpCode { get; set; } = string.Empty;
    public DateTime? OtpExpiry { get; set; }
    public bool IsActive { get; set; }

    // --- NEW FIELDS ---
    public string AvatarUrl { get; set; } = string.Empty;
    public UserPreferences Preferences { get; set; } = new();
    public UserProfileData ProfileData { get; set; } = new();
}

public record UserProfileResponse(
    int Id,
    string Name,
    string Username,
    string Email,
    string PhoneNumber,
    string Role,
    string AvatarUrl,
    UserPreferences Preferences,
    UserProfileData ProfileData
);

// 2. Complex Types (Stored as JSONB)
public class UserPreferences
{
    public string Theme { get; set; } = "system"; // system, light, dark
    public string Language { get; set; } = "en";
    public bool TwoFactorEnabled { get; set; }
    public NotificationPrefs Notifications { get; set; } = new();
}

public class NotificationPrefs
{
    public bool Email { get; set; } = true;
    public bool Push { get; set; } = false;
    public bool Marketing { get; set; } = true;
}

public class UserProfileData
{
    // Customer Specific
    public string ShippingAddress { get; set; } = "";
    public string BillingAddress { get; set; } = "";
    public int LoyaltyPoints { get; set; } = 0;

    // --- NEW: Multiple Address Manager ---
    public List<UserAddress> SavedAddresses { get; set; } = new();

    // Admin/Seller Specific
    public string StoreName { get; set; } = "";
    public string Description { get; set; } = "";
    public string TaxId { get; set; } = "";
    public string BankAccount { get; set; } = "";
    public SocialLinks Socials { get; set; } = new();
    public SellerOps SellerOps { get; set; } = new();
}

// --- NEW: Address Model ---
public class UserAddress
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Label { get; set; } = "Home"; // e.g., Home, Work, Mom's
    public string Value { get; set; } = "";     // The actual address string
}
public class SocialLinks
{
    public string Instagram { get; set; } = "";
    public string Facebook { get; set; } = "";
}

public class SellerOps
{
    public bool AutoAcceptOrders { get; set; } = true;
    public string Currency { get; set; } = "USD";
}

// 3. Request DTOs
public record RegisterRequest(string Name, string Username, string Email, string PhoneNumber, string? Password = null);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token, string Role, string Name);
public record VerifyOtpRequest(string Email, string Otp, string NewPassword);
public record ForgotPasswordRequest(string Email);

public record UpdateAdminRequest(string Name, string Email, string PhoneNumber, bool IsActive);

// 4. Profile API DTOs
public record UpdateProfileRequest(
    string Name,
    string PhoneNumber,
    string AvatarUrl,
    UserProfileData ProfileData // Pass the whole object or partials
);

public record UpdatePreferencesRequest(UserPreferences Preferences);
public record AdminDto(int Id, string Name, string Username, string Email, string PhoneNumber, bool IsActive);