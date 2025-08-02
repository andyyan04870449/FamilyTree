using BCrypt.Net;

namespace familytree_backend.Tools
{
    /// <summary>
    /// 密碼雜湊產生工具
    /// 用於產生初始管理員密碼
    /// </summary>
    public class PasswordHashGenerator
    {
        /// <summary>
        /// 產生密碼雜湊
        /// </summary>
        public static string GenerateHash(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        /// <summary>
        /// 驗證密碼
        /// </summary>
        public static bool VerifyPassword(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }

        /// <summary>
        /// 產生預設管理員密碼的 SQL 更新語句
        /// </summary>
        public static string GenerateAdminPasswordUpdateSql(string password = "Admin@123")
        {
            var hash = GenerateHash(password);
            return $@"
-- 更新管理員密碼
-- 密碼: {password}
UPDATE users 
SET password_hash = '{hash}'
WHERE username = 'admin';
";
        }
    }
}