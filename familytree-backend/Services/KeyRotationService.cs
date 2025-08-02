using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using familytree_backend.Models;
using Dapper;
using Npgsql;

namespace familytree_backend.Services
{
    /// <summary>
    /// 密鑰輪換服務介面
    /// </summary>
    public interface IKeyRotationService
    {
        /// <summary>
        /// 獲取當前可用的密鑰
        /// </summary>
        /// <returns>密鑰輪換資訊</returns>
        Task<JwtKeyRotation> GetCurrentKeysAsync();

        /// <summary>
        /// 執行密鑰輪換
        /// </summary>
        Task RotateKeysAsync();

        /// <summary>
        /// 檢查是否應該輪換密鑰
        /// </summary>
        /// <returns>是否需要輪換</returns>
        Task<bool> ShouldRotateAsync();

        /// <summary>
        /// 強制初始化密鑰（僅在首次運行時使用）
        /// </summary>
        Task InitializeKeysAsync();

        /// <summary>
        /// 清理過期的舊密鑰
        /// </summary>
        Task CleanupExpiredKeysAsync();

        /// <summary>
        /// 獲取所有有效的簽名密鑰（包含當前和過渡期的舊密鑰）
        /// </summary>
        Task<List<string>> GetAllValidSigningKeysAsync();
    }

    /// <summary>
    /// 密鑰輪換服務實作
    /// </summary>
    public class KeyRotationService : IKeyRotationService
    {
        private readonly KeyRotationSettings _settings;
        private readonly string _connectionString;
        private readonly ILogger<KeyRotationService> _logger;
        private readonly object _lockObject = new object();

        public KeyRotationService(
            IOptions<KeyRotationSettings> settings,
            IConfigurationService configurationService,
            ILogger<KeyRotationService> logger)
        {
            _settings = settings.Value;
            _connectionString = configurationService.GetConnectionString();
            _logger = logger;
        }

        public async Task<JwtKeyRotation> GetCurrentKeysAsync()
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                var sql = @"
                    SELECT current_key_id, current_key, current_key_expiry, 
                           next_key_id, next_key, next_key_activation,
                           version, created_at, updated_at
                    FROM jwt_key_rotation 
                    WHERE current_key_expiry > CURRENT_TIMESTAMP
                    ORDER BY version DESC
                    LIMIT 1";

                var keyRotation = await connection.QuerySingleOrDefaultAsync<JwtKeyRotation>(sql);

                // 如果沒有找到有效的密鑰，初始化一個新的
                if (keyRotation == null)
                {
                    _logger.LogWarning("No valid JWT keys found, initializing new keys");
                    await InitializeKeysAsync();
                    return await GetCurrentKeysAsync();
                }

                return keyRotation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current JWT keys");
                throw;
            }
        }

        public async Task RotateKeysAsync()
        {
            lock (_lockObject)
            {
                // 防止並發輪換
            }

            try
            {
                _logger.LogInformation("Starting JWT key rotation");

                var currentKeys = await GetCurrentKeysAsync();
                
                // 如果已經有下一個密鑰準備好，就啟用它
                if (!string.IsNullOrEmpty(currentKeys.NextKey))
                {
                    await ActivateNextKeyAsync(currentKeys);
                }
                else
                {
                    // 生成新的密鑰對
                    await GenerateNewKeyPairAsync(currentKeys);
                }

                _logger.LogInformation("JWT key rotation completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during JWT key rotation");
                throw;
            }
        }

        public async Task<bool> ShouldRotateAsync()
        {
            try
            {
                if (!_settings.EnableAutoRotation)
                    return false;

                var currentKeys = await GetCurrentKeysAsync();
                var rotationThreshold = DateTime.UtcNow.AddDays(_settings.RotationAdvanceDays);

                return currentKeys.CurrentKeyExpiry <= rotationThreshold;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if key rotation is needed");
                return false;
            }
        }

        public async Task InitializeKeysAsync()
        {
            try
            {
                _logger.LogInformation("Initializing JWT keys");

                await EnsureKeyRotationTableExistsAsync();

                // 檢查是否已經有密鑰
                using var connection = new NpgsqlConnection(_connectionString);
                var existingCount = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM jwt_key_rotation");

                if (existingCount > 0)
                {
                    _logger.LogInformation("JWT keys already exist, skipping initialization");
                    return;
                }

                // 生成初始密鑰
                var keyId = Guid.NewGuid().ToString();
                var secret = JwtSecretValidator.GenerateSecureSecret(_settings.MinKeyLength);
                var expiry = DateTime.UtcNow.AddDays(_settings.KeyValidityDays);

                // 驗證生成的密鑰強度
                if (!JwtSecretValidator.ValidateSecret(secret))
                {
                    throw new InvalidOperationException("Generated JWT secret does not meet security requirements");
                }

                var keyRotation = new JwtKeyRotation
                {
                    CurrentKeyId = keyId,
                    CurrentKey = secret,
                    CurrentKeyExpiry = expiry,
                    Version = 1
                };

                await SaveKeyRotationAsync(keyRotation);

                _logger.LogInformation("JWT keys initialized successfully with key ID: {KeyId}", keyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing JWT keys");
                throw;
            }
        }

        public async Task CleanupExpiredKeysAsync()
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                var cutoffDate = DateTime.UtcNow.AddDays(-_settings.OldKeyRetentionDays);

                var sql = @"
                    DELETE FROM jwt_key_rotation 
                    WHERE current_key_expiry < @CutoffDate 
                    AND updated_at < @CutoffDate";

                var affected = await connection.ExecuteAsync(sql, new { CutoffDate = cutoffDate });

                if (affected > 0)
                {
                    _logger.LogInformation("Cleaned up {Count} expired JWT key rotation records", affected);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up expired JWT keys");
                throw;
            }
        }

        public async Task<List<string>> GetAllValidSigningKeysAsync()
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                var retentionThreshold = DateTime.UtcNow.AddDays(-_settings.OldKeyRetentionDays);

                var sql = @"
                    SELECT DISTINCT unnest(ARRAY[current_key, next_key]) as signing_key
                    FROM jwt_key_rotation 
                    WHERE (current_key_expiry > @RetentionThreshold OR updated_at > @RetentionThreshold)
                    AND unnest(ARRAY[current_key, next_key]) IS NOT NULL";

                var keys = await connection.QueryAsync<string>(sql, new { RetentionThreshold = retentionThreshold });
                return keys.Where(k => !string.IsNullOrEmpty(k)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all valid signing keys");
                throw;
            }
        }

        private async Task ActivateNextKeyAsync(JwtKeyRotation currentKeys)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // 將 next_key 變為 current_key
                var newKeyRotation = new JwtKeyRotation
                {
                    CurrentKeyId = currentKeys.NextKeyId!,
                    CurrentKey = currentKeys.NextKey!,
                    CurrentKeyExpiry = DateTime.UtcNow.AddDays(_settings.KeyValidityDays),
                    Version = currentKeys.Version + 1
                };

                await SaveKeyRotationAsync(newKeyRotation, transaction);
                await transaction.CommitAsync();

                _logger.LogInformation("Activated next JWT key with ID: {KeyId}", newKeyRotation.CurrentKeyId);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error activating next JWT key");
                throw;
            }
        }

        private async Task GenerateNewKeyPairAsync(JwtKeyRotation currentKeys)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // 生成新的密鑰
                var newKeyId = Guid.NewGuid().ToString();
                var newSecret = JwtSecretValidator.GenerateSecureSecret(_settings.MinKeyLength);

                if (!JwtSecretValidator.ValidateSecret(newSecret))
                {
                    throw new InvalidOperationException("Generated JWT secret does not meet security requirements");
                }

                var newKeyRotation = new JwtKeyRotation
                {
                    CurrentKeyId = newKeyId,
                    CurrentKey = newSecret,
                    CurrentKeyExpiry = DateTime.UtcNow.AddDays(_settings.KeyValidityDays),
                    Version = currentKeys.Version + 1
                };

                await SaveKeyRotationAsync(newKeyRotation, transaction);
                await transaction.CommitAsync();

                _logger.LogInformation("Generated new JWT key pair with ID: {KeyId}", newKeyId);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error generating new JWT key pair");
                throw;
            }
        }

        private async Task SaveKeyRotationAsync(JwtKeyRotation keyRotation, NpgsqlTransaction? transaction = null)
        {
            var connection = transaction?.Connection ?? new NpgsqlConnection(_connectionString);
            var shouldDisposeConnection = transaction == null;

            try
            {
                if (transaction == null)
                    await connection.OpenAsync();

                var sql = @"
                    INSERT INTO jwt_key_rotation 
                    (current_key_id, current_key, current_key_expiry, next_key_id, next_key, next_key_activation, version, created_at, updated_at)
                    VALUES (@CurrentKeyId, @CurrentKey, @CurrentKeyExpiry, @NextKeyId, @NextKey, @NextKeyActivation, @Version, @CreatedAt, @UpdatedAt)";

                await connection.ExecuteAsync(sql, keyRotation, transaction);
            }
            finally
            {
                if (shouldDisposeConnection)
                    connection?.Dispose();
            }
        }

        private async Task EnsureKeyRotationTableExistsAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            
            var createTableSql = @"
                CREATE TABLE IF NOT EXISTS jwt_key_rotation (
                    id SERIAL PRIMARY KEY,
                    current_key_id VARCHAR(50) NOT NULL,
                    current_key TEXT NOT NULL,
                    current_key_expiry TIMESTAMP NOT NULL,
                    next_key_id VARCHAR(50),
                    next_key TEXT,
                    next_key_activation TIMESTAMP,
                    version INTEGER NOT NULL DEFAULT 1,
                    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
                );

                CREATE INDEX IF NOT EXISTS idx_jwt_key_rotation_expiry ON jwt_key_rotation(current_key_expiry);
                CREATE INDEX IF NOT EXISTS idx_jwt_key_rotation_version ON jwt_key_rotation(version);";

            await connection.ExecuteAsync(createTableSql);
        }
    }
}