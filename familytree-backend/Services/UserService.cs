using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using familytree_backend.Models;
using Microsoft.Extensions.Logging;
using BCrypt.Net;

namespace familytree_backend.Services
{
    public interface IUserService
    {
        Task<UserModel?> GetByIdAsync(string userId);
        Task<UserModel?> GetByUsernameAsync(string username);
        Task<UserModel?> GetByEmailAsync(string email);
        Task<UserModel?> CreateAsync(RegisterDto dto);
        Task<UserModel?> UpdateAsync(string userId, UpdateUserDto dto);
        Task<bool> UpdatePasswordAsync(string userId, string newPasswordHash);
        Task<bool> UpdateLastLoginAsync(string userId);
        Task<bool> ChangePasswordAsync(string userId, string oldPassword, string newPassword);
        Task<PagedResult<UserModel>> GetUsersAsync(int page, int pageSize);
        Task<bool> ValidatePasswordAsync(string userId, string password);
        Task<bool> UserExistsAsync(string username, string email);
        Task<string> GenerateTemporaryPasswordAsync();
    }

    public class UserService : IUserService
    {
        private readonly string _connectionString;
        private readonly ILogger<UserService> _logger;
        private readonly ILoggingService _loggingService;

        public UserService(
            IConfigurationService configurationService,
            ILogger<UserService> logger,
            ILoggingService loggingService)
        {
            _connectionString = configurationService.GetConnectionString();
            _logger = logger;
            _loggingService = loggingService;
        }

        public async Task<UserModel?> GetByIdAsync(string userId)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                var sql = @"
                    SELECT id, username, email, full_name, role, status, 
                           created_at, updated_at, last_login_at
                    FROM users 
                    WHERE id = @UserId AND status = 'active'";

                var user = await connection.QuerySingleOrDefaultAsync<UserModel>(sql, new { UserId = userId });
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<UserModel?> GetByUsernameAsync(string username)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                var sql = @"
                    SELECT id, username, email, full_name, role, status, 
                           created_at, updated_at, last_login_at
                    FROM users 
                    WHERE username = @Username AND status = 'active'";

                var user = await connection.QuerySingleOrDefaultAsync<UserModel>(sql, new { Username = username });
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by username: {Username}", username);
                throw;
            }
        }

        public async Task<UserModel?> GetByEmailAsync(string email)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                var sql = @"
                    SELECT id, username, email, full_name, role, status, 
                           created_at, updated_at, last_login_at
                    FROM users 
                    WHERE email = @Email AND status = 'active'";

                var user = await connection.QuerySingleOrDefaultAsync<UserModel>(sql, new { Email = email });
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by email: {Email}", email);
                throw;
            }
        }

        public async Task<UserModel?> CreateAsync(RegisterDto dto)
        {
            try
            {
                // 檢查使用者是否已存在
                if (await UserExistsAsync(dto.Username, dto.Email))
                {
                    _logger.LogWarning("User already exists: {Username} or {Email}", dto.Username, dto.Email);
                    return null;
                }

                // 加密密碼
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

                using var connection = new NpgsqlConnection(_connectionString);
                var sql = @"
                    INSERT INTO users (username, email, password_hash, full_name, role, status)
                    VALUES (@Username, @Email, @PasswordHash, @FullName, 'user', 'active')
                    RETURNING id, username, email, full_name, role, status, created_at, updated_at, last_login_at";

                var user = await connection.QuerySingleAsync<UserModel>(sql, new
                {
                    dto.Username,
                    dto.Email,
                    PasswordHash = passwordHash,
                    dto.FullName
                });

                // 記錄活動日誌
                await _loggingService.LogActivityAsync(
                    user.Id,
                    "user_register",
                    $"New user registered: {user.Username}"
                );

                _logger.LogInformation("User created successfully: {UserId}", user.Id);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                throw;
            }
        }

        public async Task<UserModel?> UpdateAsync(string userId, UpdateUserDto dto)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                
                // 建立動態更新語句
                var updates = new List<string>();
                var parameters = new DynamicParameters();
                parameters.Add("UserId", userId);

                if (!string.IsNullOrEmpty(dto.Email))
                {
                    updates.Add("email = @Email");
                    parameters.Add("Email", dto.Email);
                }

                if (!string.IsNullOrEmpty(dto.FullName))
                {
                    updates.Add("full_name = @FullName");
                    parameters.Add("FullName", dto.FullName);
                }

                if (!string.IsNullOrEmpty(dto.Status))
                {
                    updates.Add("status = @Status");
                    parameters.Add("Status", dto.Status);
                }

                if (!string.IsNullOrEmpty(dto.Role))
                {
                    updates.Add("role = @Role");
                    parameters.Add("Role", dto.Role);
                }

                if (updates.Count == 0)
                {
                    return await GetByIdAsync(userId);
                }

                var sql = $@"
                    UPDATE users 
                    SET {string.Join(", ", updates)}
                    WHERE id = @UserId
                    RETURNING id, username, email, full_name, role, status, created_at, updated_at, last_login_at";

                var user = await connection.QuerySingleOrDefaultAsync<UserModel>(sql, parameters);

                if (user != null)
                {
                    _logger.LogInformation("User updated successfully: {UserId}", userId);
                }

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> UpdatePasswordAsync(string userId, string newPasswordHash)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                var sql = @"
                    UPDATE users 
                    SET password_hash = @PasswordHash
                    WHERE id = @UserId";

                var affected = await connection.ExecuteAsync(sql, new
                {
                    UserId = userId,
                    PasswordHash = newPasswordHash
                });

                return affected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating password for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> UpdateLastLoginAsync(string userId)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                var sql = @"
                    UPDATE users 
                    SET last_login_at = CURRENT_TIMESTAMP
                    WHERE id = @UserId";

                var affected = await connection.ExecuteAsync(sql, new { UserId = userId });
                return affected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating last login for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> ChangePasswordAsync(string userId, string oldPassword, string newPassword)
        {
            try
            {
                // 驗證舊密碼
                if (!await ValidatePasswordAsync(userId, oldPassword))
                {
                    _logger.LogWarning("Invalid old password for user: {UserId}", userId);
                    return false;
                }

                // 更新密碼
                var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                var success = await UpdatePasswordAsync(userId, newPasswordHash);

                if (success)
                {
                    await _loggingService.LogActivityAsync(
                        userId,
                        "change_password",
                        "User changed password"
                    );
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<PagedResult<UserModel>> GetUsersAsync(int page, int pageSize)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                
                // 計算總數
                var countSql = "SELECT COUNT(*) FROM users WHERE status != 'deleted'";
                var totalCount = await connection.ExecuteScalarAsync<int>(countSql);

                // 查詢分頁資料
                var offset = (page - 1) * pageSize;
                var sql = @"
                    SELECT id, username, email, full_name, role, status, 
                           created_at, updated_at, last_login_at
                    FROM users 
                    WHERE status != 'deleted'
                    ORDER BY created_at DESC
                    LIMIT @PageSize OFFSET @Offset";

                var users = await connection.QueryAsync<UserModel>(sql, new
                {
                    PageSize = pageSize,
                    Offset = offset
                });

                return new PagedResult<UserModel>
                {
                    Data = users,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users");
                throw;
            }
        }

        public async Task<bool> ValidatePasswordAsync(string userId, string password)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                var sql = "SELECT password_hash FROM users WHERE id = @UserId AND status = 'active'";
                var passwordHash = await connection.QuerySingleOrDefaultAsync<string>(sql, new { UserId = userId });

                if (string.IsNullOrEmpty(passwordHash))
                {
                    return false;
                }

                return BCrypt.Net.BCrypt.Verify(password, passwordHash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating password for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> UserExistsAsync(string username, string email)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                var sql = @"
                    SELECT COUNT(*) 
                    FROM users 
                    WHERE (username = @Username OR email = @Email) 
                    AND status != 'deleted'";

                var count = await connection.ExecuteScalarAsync<int>(sql, new
                {
                    Username = username,
                    Email = email
                });

                return count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user exists");
                throw;
            }
        }

        public Task<string> GenerateTemporaryPasswordAsync()
        {
            // 產生臨時密碼
            var random = new Random();
            var password = $"Tmp{DateTime.Now:yyyyMMdd}!{random.Next(1000, 9999)}";
            return Task.FromResult(password);
        }
    }
}