-- 更新管理員密碼
-- 預設密碼: Admin@123
-- BCrypt hash for Admin@123 (需要在 C# 應用程式中產生真實的 hash)

-- 這是一個範例，實際的 hash 需要用 BCrypt 產生
-- 在 C# 中使用: BCrypt.Net.BCrypt.HashPassword("Admin@123")
UPDATE users 
SET password_hash = '$2b$10$PLACEHOLDER_HASH_NEEDS_TO_BE_GENERATED'
WHERE username = 'admin';

-- 註：請在應用程式中執行以下 C# 程式碼來產生真實的密碼 hash：
-- var passwordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
-- 然後將產生的 hash 替換上面的 PLACEHOLDER_HASH_NEEDS_TO_BE_GENERATED