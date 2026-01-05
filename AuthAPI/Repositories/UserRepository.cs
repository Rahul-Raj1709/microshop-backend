using AuthAPI.Data;
using AuthAPI.Models;
using Dapper;

namespace AuthAPI.Repositories;

public interface IUserRepository
{
    Task<User?> GetUserByEmail(string email);
    Task CreateUser(User user);
    Task UpdateUserOtp(int userId, string otp, DateTime expiry);
    Task ActivateUserAndSetPassword(int userId, string passwordHash);
    Task<IEnumerable<User>> GetUsersByRole(string role);
    Task<int> DeleteUser(int id);
    Task<int> UpdateUser(User user);
}

public class UserRepository : IUserRepository
{
    private readonly DapperContext _context;

    public UserRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<User?> GetUserByEmail(string email)
    {
        // Added: name AS Name
        var sql = @"
            SELECT 
                id, name AS Name, username, email, 
                phone_number AS PhoneNumber, password_hash AS PasswordHash, 
                role, otp_code AS OtpCode, otp_expiry AS OtpExpiry, 
                is_active AS IsActive 
            FROM users WHERE email = @Email";

        using var connection = _context.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<User>(sql, new { email });
    }

    public async Task CreateUser(User user)
    {
        // Added: name column and @Name parameter
        var sql = @"
            INSERT INTO users (name, username, email, phone_number, role, otp_code, otp_expiry, is_active, password_hash) 
            VALUES (@Name, @Username, @Email, @PhoneNumber, @Role, @OtpCode, @OtpExpiry, @IsActive, @PasswordHash)";

        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, user);
    }

    public async Task<IEnumerable<User>> GetUsersByRole(string role)
    {
        // Added: name AS Name
        var sql = @"
            SELECT 
                id, name AS Name, username, email, 
                phone_number AS PhoneNumber, password_hash AS PasswordHash, 
                role, otp_code AS OtpCode, otp_expiry AS OtpExpiry, 
                is_active AS IsActive 
            FROM users 
            WHERE role = @Role";

        using var connection = _context.CreateConnection();
        return await connection.QueryAsync<User>(sql, new { Role = role });
    }

    public async Task<int> UpdateUser(User user)
    {
        // Added: name = @Name
        var sql = @"
            UPDATE users 
            SET name = @Name,
                email = @Email, 
                phone_number = @PhoneNumber, 
                is_active = @IsActive 
            WHERE id = @Id";

        using var connection = _context.CreateConnection();
        return await connection.ExecuteAsync(sql, user);
    }

    // (These methods remain unchanged but included for completeness)
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