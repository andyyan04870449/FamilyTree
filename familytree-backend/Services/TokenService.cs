using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using familytree_backend.Models;
using Dapper;
using Npgsql;
using Microsoft.Extensions.Logging;

namespace familytree_backend.Services
{
    public interface ITokenService
    {
        string GenerateAccessToken(UserModel user);
        string GenerateRefreshToken();
        Task<bool> SaveRefreshTokenAsync(string userId, string token, DateTime expiresAt);
        Task<bool> ValidateRefreshTokenAsync(string token);
        Task<bool> RevokeRefreshTokenAsync(string token);
        Task<UserTokenModel?> GetRefreshTokenAsync(string token);
        Task<bool> CleanupExpiredTokensAsync();
        ClaimsPrincipal? ValidateToken(string token);
    }

    /// <summary>
    /// JWT 密鑰強度驗證器
    /// </summary>
    public static class JwtSecretValidator
    {
        /// <summary>
        /// 驗證 JWT 密鑰強度
        /// </summary>
        /// <param name="secret">要驗證的密鑰</param>
        /// <returns>密鑰是否符合安全要求</returns>
        public static bool ValidateSecret(string secret)
        {
            if (string.IsNullOrEmpty(secret))
                return false;
                
            // 至少 32 字符（256 位）
            if (secret.Length < 32)
                return false;
                
            // 檢查複雜度：必須包含大寫字母、小寫字母、數字和特殊字符
            var hasUpper = secret.Any(char.IsUpper);
            var hasLower = secret.Any(char.IsLower);
            var hasDigit = secret.Any(char.IsDigit);
            var hasSpecial = secret.Any(ch => !char.IsLetterOrDigit(ch));
            
            return hasUpper && hasLower && hasDigit && hasSpecial;
        }

        /// <summary>
        /// 獲取密鑰強度評分
        /// </summary>
        /// <param name="secret">要評分的密鑰</param>
        /// <returns>密鑰強度評分 (0-100)</returns>
        public static int GetSecretStrengthScore(string secret)
        {
            if (string.IsNullOrEmpty(secret))
                return 0;

            int score = 0;

            // 長度評分 (最多 40 分)
            if (secret.Length >= 32) score += 20;
            if (secret.Length >= 48) score += 10;
            if (secret.Length >= 64) score += 10;

            // 複雜度評分 (最多 60 分)
            if (secret.Any(char.IsUpper)) score += 15;
            if (secret.Any(char.IsLower)) score += 15;
            if (secret.Any(char.IsDigit)) score += 15;
            if (secret.Any(ch => !char.IsLetterOrDigit(ch))) score += 15;

            return Math.Min(score, 100);
        }

        /// <summary>
        /// 生成建議的安全密鑰
        /// </summary>
        /// <param name="length">密鑰長度（預設 64）</param>
        /// <returns>隨機生成的安全密鑰</returns>
        public static string GenerateSecureSecret(int length = 64)
        {
            const string upperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowerCase = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string specialChars = "!@#$%^&*()_+-=[]{}|;:,.<>?";
            const string allChars = upperCase + lowerCase + digits + specialChars;

            using var rng = RandomNumberGenerator.Create();
            var result = new StringBuilder(length);
            var buffer = new byte[4];

            // 確保至少包含每種字符類型
            result.Append(GetRandomChar(upperCase, rng, buffer));
            result.Append(GetRandomChar(lowerCase, rng, buffer));
            result.Append(GetRandomChar(digits, rng, buffer));
            result.Append(GetRandomChar(specialChars, rng, buffer));

            // 填充剩餘長度
            for (int i = 4; i < length; i++)
            {
                result.Append(GetRandomChar(allChars, rng, buffer));
            }

            // 打亂字符順序
            return new string(result.ToString().ToCharArray().OrderBy(x => GetRandomInt(rng, buffer)).ToArray());
        }

        private static char GetRandomChar(string chars, RandomNumberGenerator rng, byte[] buffer)
        {
            rng.GetBytes(buffer);
            var randomValue = BitConverter.ToUInt32(buffer, 0);
            return chars[(int)(randomValue % chars.Length)];
        }

        private static int GetRandomInt(RandomNumberGenerator rng, byte[] buffer)
        {
            rng.GetBytes(buffer);
            return BitConverter.ToInt32(buffer, 0);
        }
    }

    public class TokenService : ITokenService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly string _connectionString;
        private readonly ILogger<TokenService> _logger;

        public TokenService(
            IOptions<JwtSettings> jwtSettings,
            IConfigurationService configurationService,
            ILogger<TokenService> logger)
        {
            _jwtSettings = jwtSettings.Value;
            _connectionString = configurationService.GetConnectionString();
            _logger = logger;

            // 驗證 JWT 密鑰強度
            ValidateJwtSecret();
        }

        /// <summary>
        /// 驗證 JWT 密鑰強度
        /// </summary>
        private void ValidateJwtSecret()
        {
            if (string.IsNullOrEmpty(_jwtSettings.Secret))
            {
                var error = "JWT Secret is not configured in TokenService";
                _logger.LogError(error);
                throw new InvalidOperationException(error);
            }

            if (!JwtSecretValidator.ValidateSecret(_jwtSettings.Secret))
            {
                var score = JwtSecretValidator.GetSecretStrengthScore(_jwtSettings.Secret);
                var error = $"JWT Secret does not meet security requirements. Strength score: {score}/100. " +
                    "Secret must be at least 32 characters long and contain uppercase, lowercase, digits, and special characters.";
                
                _logger.LogError(error);
                throw new InvalidOperationException(error);
            }

            var strengthScore = JwtSecretValidator.GetSecretStrengthScore(_jwtSettings.Secret);
            _logger.LogInformation("JWT Secret validation passed in TokenService. Strength score: {Score}/100", strengthScore);
        }

        public string GenerateAccessToken(UserModel user)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);
                
                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim("fullName", user.FullName ?? ""),
                    new Claim("status", user.Status)
                };

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiration),
                    Issuer = _jwtSettings.Issuer,
                    Audience = _jwtSettings.Audience,
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(key),
                        SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                return tokenHandler.WriteToken(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating access token for user: {UserId}", user.Id);
                throw;
            }
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public async Task<bool> SaveRefreshTokenAsync(string userId, string token, DateTime expiresAt)
        {
            try
            {
                // 計算 token hash
                var tokenHash = ComputeSha256Hash(token);

                using var connection = new NpgsqlConnection(_connectionString);
                var sql = @"
                    INSERT INTO user_tokens (user_id, token_type, token_hash, expires_at)
                    VALUES (@UserId, 'refresh', @TokenHash, @ExpiresAt)";

                var affected = await connection.ExecuteAsync(sql, new
                {
                    UserId = userId,
                    TokenHash = tokenHash,
                    ExpiresAt = expiresAt
                });

                return affected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving refresh token for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> ValidateRefreshTokenAsync(string token)
        {
            try
            {
                var tokenHash = ComputeSha256Hash(token);

                using var connection = new NpgsqlConnection(_connectionString);
                var sql = @"
                    SELECT COUNT(*) 
                    FROM user_tokens 
                    WHERE token_hash = @TokenHash 
                    AND token_type = 'refresh'
                    AND expires_at > CURRENT_TIMESTAMP";

                var count = await connection.ExecuteScalarAsync<int>(sql, new { TokenHash = tokenHash });
                return count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating refresh token");
                throw;
            }
        }

        public async Task<bool> RevokeRefreshTokenAsync(string token)
        {
            try
            {
                var tokenHash = ComputeSha256Hash(token);

                using var connection = new NpgsqlConnection(_connectionString);
                
                // 刪除 token（或者可以加一個 revoked_at 欄位）
                var sql = @"
                    DELETE FROM user_tokens 
                    WHERE token_hash = @TokenHash 
                    AND token_type = 'refresh'";

                var affected = await connection.ExecuteAsync(sql, new { TokenHash = tokenHash });
                return affected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking refresh token");
                throw;
            }
        }

        public async Task<UserTokenModel?> GetRefreshTokenAsync(string token)
        {
            try
            {
                var tokenHash = ComputeSha256Hash(token);

                using var connection = new NpgsqlConnection(_connectionString);
                var sql = @"
                    SELECT id, user_id, token_type, token_hash, expires_at, created_at, used_at
                    FROM user_tokens 
                    WHERE token_hash = @TokenHash 
                    AND token_type = 'refresh'
                    AND expires_at > CURRENT_TIMESTAMP";

                var userToken = await connection.QuerySingleOrDefaultAsync<UserTokenModel>(sql, new { TokenHash = tokenHash });

                // 更新使用時間
                if (userToken != null)
                {
                    await connection.ExecuteAsync(
                        "UPDATE user_tokens SET used_at = CURRENT_TIMESTAMP WHERE id = @Id",
                        new { Id = userToken.Id }
                    );
                }

                return userToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting refresh token");
                throw;
            }
        }

        public async Task<bool> CleanupExpiredTokensAsync()
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                var sql = @"
                    DELETE FROM user_tokens 
                    WHERE expires_at < CURRENT_TIMESTAMP";

                var affected = await connection.ExecuteAsync(sql);
                
                if (affected > 0)
                {
                    _logger.LogInformation("Cleaned up {Count} expired tokens", affected);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up expired tokens");
                return false;
            }
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                return principal;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Token validation failed: {Message}", ex.Message);
                return null;
            }
        }

        private static string ComputeSha256Hash(string rawData)
        {
            using var sha256Hash = SHA256.Create();
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            
            var builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }
}