using AuthAPI.Data;
using AuthAPI.Models;
using Dapper;

namespace AuthAPI.Repositories;

public interface IUserRepository
{
    Task<User?> GetUserByEmail(string email);
    Task<User?> GetUserById(int id);
    Task<IEnumerable<User>> GetUsersByRole(string role);
    Task CreateUser(User user);
    Task UpdateUser(User user); // General update
    Task DeleteUser(int id);
    Task UpdateUserOtp(int userId, string otp, DateTime expiry);
    Task ActivateUserAndSetPassword(int userId, string passwordHash);
    Task SaveRefreshToken(int userId, string refreshToken);
    Task<IEnumerable<UserAddress>> GetAddresses(int userId);
    Task AddAddress(UserAddress address);
    Task DeleteAddress(int addressId, int userId);
    Task UpdatePassword(int userId, string newPasswordHash);
    Task UpdateUserPreferences(int userId, UserPreferences prefs);
}

public class UserRepository : IUserRepository
{
    private readonly DapperContext _context;

    public UserRepository(DapperContext context)
    {
        _context = context;
    }

    private const string SelectUser = @"
        SELECT 
            id, username, email, password_hash AS PasswordHash, role, 
            name, phone_number AS PhoneNumber, avatar_url AS AvatarUrl, 
            is_active AS IsActive, otp_code AS OtpCode, otp_expiry AS OtpExpiry, 
            refresh_token AS RefreshToken, preferences
        FROM users";

    public async Task<User?> GetUserByEmail(string email)
    {
        var sql = $"{SelectUser} WHERE email = @Email";
        using var connection = _context.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<User>(sql, new { email });
    }

    public async Task<User?> GetUserById(int id)
    {
        var sql = $"{SelectUser} WHERE id = @Id";
        using var connection = _context.CreateConnection();
        var user = await connection.QuerySingleOrDefaultAsync<User>(sql, new { id });

        if (user != null)
        {
            user.Addresses = (await GetAddresses(id)).ToList();
        }
        return user;
    }

    public async Task<IEnumerable<User>> GetUsersByRole(string role)
    {
        var sql = @"
            SELECT 
                id, username, email, password_hash AS PasswordHash, role, 
                name, phone_number AS PhoneNumber, avatar_url AS AvatarUrl, 
                is_active AS IsActive, otp_code AS OtpCode, otp_expiry AS OtpExpiry, 
                refresh_token AS RefreshToken, preferences
            FROM users 
            WHERE role = @Role";

        using var connection = _context.CreateConnection();
        return await connection.QueryAsync<User>(sql, new { Role = role });
    }

    public async Task CreateUser(User user)
    {
        var sql = @"
            INSERT INTO users (
                username, email, password_hash, role, name, phone_number, 
                avatar_url, is_active, otp_code, otp_expiry, preferences
            ) 
            VALUES (
                @Username, @Email, @PasswordHash, @Role, @Name, @PhoneNumber, 
                @AvatarUrl, @IsActive, @OtpCode, @OtpExpiry, @Preferences::jsonb
            )
            RETURNING id";

        using var connection = _context.CreateConnection();
        user.Id = await connection.ExecuteScalarAsync<int>(sql, user);
    }

    public async Task UpdateUser(User user)
    {
        var sql = @"
            UPDATE users 
            SET name = @Name, phone_number = @PhoneNumber, avatar_url = @AvatarUrl
            WHERE id = @Id";

        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, user);
    }
    public async Task DeleteUser(int id)
    {
        var sql = "DELETE FROM users WHERE id = @Id";
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task UpdateUserOtp(int userId, string otp, DateTime expiry)
    {
        var sql = "UPDATE users SET otp_code = @Otp, otp_expiry = @Expiry WHERE id = @Id";
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, new { Otp = otp, Expiry = expiry, Id = userId });
    }
    public async Task UpdatePassword(int userId, string newPasswordHash)
    {
        var sql = "UPDATE users SET password_hash = @Hash WHERE id = @Id";
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, new { Hash = newPasswordHash, Id = userId });
    }

    public async Task ActivateUserAndSetPassword(int userId, string passwordHash)
    {
        var sql = @"
            UPDATE users 
            SET password_hash = @Hash, is_active = TRUE, otp_code = NULL, otp_expiry = NULL 
            WHERE id = @Id";
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, new { Hash = passwordHash, Id = userId });
    }

    public async Task SaveRefreshToken(int userId, string refreshToken)
    {
        var sql = "UPDATE users SET refresh_token = @Token WHERE id = @Id";
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, new { Token = refreshToken, Id = userId });
    }

    public async Task UpdateUserPreferences(int userId, UserPreferences prefs)
    {
        var sql = "UPDATE users SET preferences = @Prefs::jsonb WHERE id = @Id";
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, new { Prefs = prefs, Id = userId });
    }

    // --- Address Handling ---

    public async Task<IEnumerable<UserAddress>> GetAddresses(int userId)
    {
        var sql = @"
            SELECT id, user_id AS UserId, label, address_line AS AddressLine, is_default AS IsDefault 
            FROM user_addresses 
            WHERE user_id = @UserId";
        using var connection = _context.CreateConnection();
        return await connection.QueryAsync<UserAddress>(sql, new { UserId = userId });
    }

    public async Task AddAddress(UserAddress address)
    {
        var sql = @"
            INSERT INTO user_addresses (user_id, label, address_line, is_default)
            VALUES (@UserId, @Label, @AddressLine, @IsDefault)";
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, address);
    }

    public async Task DeleteAddress(int addressId, int userId)
    {
        var sql = "DELETE FROM user_addresses WHERE id = @Id AND user_id = @UserId";
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, new { Id = addressId, UserId = userId });
    }
}