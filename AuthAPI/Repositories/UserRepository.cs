using AuthAPI.Data;
using AuthAPI.Models;
using Dapper;

namespace AuthAPI.Repositories;

public interface IUserRepository
{
    Task<User?> GetUserByEmail(string email);
    Task<User?> GetUserById(int id); // Added
    Task CreateUser(User user);
    Task UpdateUserOtp(int userId, string otp, DateTime expiry);
    Task ActivateUserAndSetPassword(int userId, string passwordHash);
    Task<IEnumerable<User>> GetUsersByRole(string role);
    Task<int> DeleteUser(int id);
    Task<int> UpdateUser(User user);

    // New Methods
    Task UpdateUserProfile(int userId, string name, string phone, string avatar, UserProfileData data);
    Task UpdateUserPreferences(int userId, UserPreferences prefs);
}

public class UserRepository : IUserRepository
{
    private readonly DapperContext _context;

    public UserRepository(DapperContext context)
    {
        _context = context;
    }

    // Helper for common SELECT fields
    private const string SelectFields = @"
        id, name, username, email, phone_number AS PhoneNumber, 
        password_hash AS PasswordHash, role, otp_code AS OtpCode, 
        otp_expiry AS OtpExpiry, is_active AS IsActive,
        avatar_url AS AvatarUrl, preferences, profile_data AS ProfileData";

    public async Task<User?> GetUserByEmail(string email)
    {
        var sql = $"SELECT {SelectFields} FROM users WHERE email = @Email";
        using var connection = _context.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<User>(sql, new { email });
    }

    public async Task<User?> GetUserById(int id)
    {
        var sql = $"SELECT {SelectFields} FROM users WHERE id = @Id";
        using var connection = _context.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<User>(sql, new { id });
    }

    public async Task CreateUser(User user)
    {
        // Added new columns
        var sql = @"
            INSERT INTO users (
                name, username, email, phone_number, role, 
                otp_code, otp_expiry, is_active, password_hash,
                avatar_url, preferences, profile_data
            ) 
            VALUES (
                @Name, @Username, @Email, @PhoneNumber, @Role, 
                @OtpCode, @OtpExpiry, @IsActive, @PasswordHash,
                @AvatarUrl, @Preferences, @ProfileData
            )";

        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, user);
    }

    public async Task UpdateUserProfile(int userId, string name, string phone, string avatar, UserProfileData data)
    {
        var sql = @"
            UPDATE users 
            SET name = @Name, 
                phone_number = @Phone, 
                avatar_url = @Avatar,
                profile_data = @Data::jsonb
            WHERE id = @Id";

        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, new
        {
            Name = name,
            Phone = phone,
            Avatar = avatar,
            Data = data,
            Id = userId
        });
    }

    public async Task UpdateUserPreferences(int userId, UserPreferences prefs)
    {
        var sql = @"UPDATE users SET preferences = @Prefs::jsonb WHERE id = @Id";
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, new { Prefs = prefs, Id = userId });
    }

    // ... (Keep existing methods: GetUsersByRole, UpdateUserOtp, etc.)

    public async Task<IEnumerable<User>> GetUsersByRole(string role)
    {
        var sql = $"SELECT {SelectFields} FROM users WHERE role = @Role";
        using var connection = _context.CreateConnection();
        return await connection.QueryAsync<User>(sql, new { Role = role });
    }

    public async Task<int> UpdateUser(User user)
    {
        // Admin update method
        var sql = @"
            UPDATE users 
            SET name = @Name, email = @Email, phone_number = @PhoneNumber, is_active = @IsActive 
            WHERE id = @Id";
        using var connection = _context.CreateConnection();
        return await connection.ExecuteAsync(sql, user);
    }

    public async Task UpdateUserOtp(int userId, string otp, DateTime expiry)
    {
        var sql = "UPDATE users SET otp_code = @Otp, otp_expiry = @Expiry WHERE id = @Id";
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, new { Otp = otp, Expiry = expiry, Id = userId });
    }

    public async Task ActivateUserAndSetPassword(int userId, string passwordHash)
    {
        var sql = "UPDATE users SET password_hash = @Hash, is_active = TRUE, otp_code = NULL, otp_expiry = NULL WHERE id = @Id";
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, new { Hash = passwordHash, Id = userId });
    }

    public async Task<int> DeleteUser(int id)
    {
        var sql = "DELETE FROM users WHERE id = @Id";
        using var connection = _context.CreateConnection();
        return await connection.ExecuteAsync(sql, new { Id = id });
    }
}