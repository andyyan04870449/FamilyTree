// 統一參數驗證服務 - 提供全面的輸入驗證和安全性檢查
// 設計改善：建立統一的參數驗證機制，強化安全性
using Microsoft.Extensions.Logging;
using familytree_backend.Constants;
using System.Text.RegularExpressions;

namespace familytree_backend.Services
{
    /// <summary>
    /// 統一參數驗證服務介面
    /// 設計理念：提供全面的輸入驗證和安全性檢查
    /// </summary>
    public interface IValidationService
    {
        /// <summary>
        /// 驗證字串參數
        /// </summary>
        ValidationResult ValidateString(string? value, string parameterName, ValidationOptions? options = null);

        /// <summary>
        /// 驗證電子郵件
        /// </summary>
        ValidationResult ValidateEmail(string? email);

        /// <summary>
        /// 驗證電話號碼
        /// </summary>
        ValidationResult ValidatePhone(string? phone);

        /// <summary>
        /// 驗證檔案名稱
        /// </summary>
        ValidationResult ValidateFileName(string? fileName);

        /// <summary>
        /// 驗證檔案路徑
        /// </summary>
        ValidationResult ValidateFilePath(string? filePath);

        /// <summary>
        /// 驗證 SQL 注入
        /// </summary>
        ValidationResult ValidateSqlInjection(string? value);

        /// <summary>
        /// 驗證 XSS 攻擊
        /// </summary>
        ValidationResult ValidateXss(string? value);

        /// <summary>
        /// 驗證檔案類型
        /// </summary>
        ValidationResult ValidateFileType(string? fileName, string[] allowedExtensions);

        /// <summary>
        /// 驗證檔案大小
        /// </summary>
        ValidationResult ValidateFileSize(long fileSize, long maxSize);

        /// <summary>
        /// 清理和正規化輸入
        /// </summary>
        string SanitizeInput(string? input, SanitizationOptions? options = null);

        /// <summary>
        /// 驗證專案 ID
        /// </summary>
        ValidationResult ValidateProjectId(string? projectId);

        /// <summary>
        /// 驗證分頁參數
        /// </summary>
        ValidationResult ValidatePagination(int page, int pageSize);

        /// <summary>
        /// 驗證搜尋關鍵字
        /// </summary>
        ValidationResult ValidateSearchKeyword(string? keyword);
    }

    /// <summary>
    /// 統一參數驗證服務實作
    /// 職責：提供全面的輸入驗證和安全性檢查，防止各種攻擊
    /// </summary>
    public class ValidationService : IValidationService
    {
        private readonly ILogger<ValidationService> _logger;
        private readonly IConfigurationService _configurationService;
        private readonly SecurityConfiguration _securityConfig;

        // 危險字元模式
        private static readonly Regex[] DangerousPatterns = new[]
        {
            new Regex(@"<script[^>]*>.*?</script>", RegexOptions.IgnoreCase | RegexOptions.Singleline),
            new Regex(@"javascript:", RegexOptions.IgnoreCase),
            new Regex(@"vbscript:", RegexOptions.IgnoreCase),
            new Regex(@"on\w+\s*=", RegexOptions.IgnoreCase),
            new Regex(@"<iframe[^>]*>.*?</iframe>", RegexOptions.IgnoreCase | RegexOptions.Singleline),
            new Regex(@"<object[^>]*>.*?</object>", RegexOptions.IgnoreCase | RegexOptions.Singleline),
            new Regex(@"<embed[^>]*>.*?</embed>", RegexOptions.IgnoreCase | RegexOptions.Singleline)
        };

        // SQL 注入模式
        private static readonly Regex[] SqlInjectionPatterns = new[]
        {
            new Regex(@"(\b(union|select|insert|update|delete|drop|create|alter|exec|execute|declare|cast|convert)\b)", RegexOptions.IgnoreCase),
            new Regex(@"(\b(or|and)\b\s+\d+\s*=\s*\d+)", RegexOptions.IgnoreCase),
            new Regex(@"(\b(or|and)\b\s+['""]\w+['""]\s*=\s*['""]\w+['""])", RegexOptions.IgnoreCase),
            new Regex(@"(\b(union|select)\b.*?\bfrom\b)", RegexOptions.IgnoreCase | RegexOptions.Singleline),
            new Regex(@"(\b(insert|update)\b.*?\binto\b)", RegexOptions.IgnoreCase | RegexOptions.Singleline),
            new Regex(@"(\b(delete)\b.*?\bfrom\b)", RegexOptions.IgnoreCase | RegexOptions.Singleline),
            new Regex(@"(\b(drop|create|alter)\b.*?\b(table|database|index)\b)", RegexOptions.IgnoreCase | RegexOptions.Singleline)
        };

        // 檔案名稱危險字元
        private static readonly Regex DangerousFileNamePattern = new Regex(@"[<>:""/\\|?*\x00-\x1f]", RegexOptions.Compiled);

        // 路徑遍歷模式
        private static readonly Regex PathTraversalPattern = new Regex(@"\.\./|\.\.\\|%2e%2e%2f|%2e%2e%5c", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// 建構子
        /// </summary>
        public ValidationService(ILogger<ValidationService> logger, IConfigurationService configurationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _securityConfig = configurationService.GetSecurityConfiguration();
        }

        /// <summary>
        /// 驗證字串參數
        /// 設計理念：統一的字串參數驗證邏輯
        /// </summary>
        public ValidationResult ValidateString(string? value, string parameterName, ValidationOptions? options = null)
        {
            var result = new ValidationResult { IsValid = true };

            // 檢查空值
            if (options?.AllowNull == false && string.IsNullOrWhiteSpace(value))
            {
                result.IsValid = false;
                result.ErrorMessage = $"{parameterName} 不能為空";
                _logger.LogWarning("字串驗證失敗 - {ParameterName}: 空值", parameterName);
                return result;
            }

            // 如果為空且允許空值，直接返回成功
            if (string.IsNullOrWhiteSpace(value))
            {
                return result;
            }

            // 檢查長度
            if (options?.MaxLength.HasValue == true && value.Length > options.MaxLength.Value)
            {
                result.IsValid = false;
                result.ErrorMessage = $"{parameterName} 長度超過限制 ({value.Length} > {options.MaxLength})";
                _logger.LogWarning("字串驗證失敗 - {ParameterName}: 長度超限", parameterName);
                return result;
            }

            if (options?.MinLength.HasValue == true && value.Length < options.MinLength.Value)
            {
                result.IsValid = false;
                result.ErrorMessage = $"{parameterName} 長度不足 ({value.Length} < {options.MinLength})";
                _logger.LogWarning("字串驗證失敗 - {ParameterName}: 長度不足", parameterName);
                return result;
            }

            // 檢查模式
            if (!string.IsNullOrEmpty(options?.Pattern) && !Regex.IsMatch(value, options.Pattern))
            {
                result.IsValid = false;
                result.ErrorMessage = $"{parameterName} 格式不正確";
                _logger.LogWarning("字串驗證失敗 - {ParameterName}: 格式不正確", parameterName);
                return result;
            }

            // 檢查 SQL 注入
            if (options?.CheckSqlInjection != false)
            {
                var sqlResult = ValidateSqlInjection(value);
                if (!sqlResult.IsValid)
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"{parameterName} 包含危險內容";
                    _logger.LogWarning("字串驗證失敗 - {ParameterName}: SQL 注入檢測", parameterName);
                    return result;
                }
            }

            // 檢查 XSS
            if (options?.CheckXss != false)
            {
                var xssResult = ValidateXss(value);
                if (!xssResult.IsValid)
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"{parameterName} 包含危險內容";
                    _logger.LogWarning("字串驗證失敗 - {ParameterName}: XSS 檢測", parameterName);
                    return result;
                }
            }

            return result;
        }

        /// <summary>
        /// 驗證電子郵件
        /// 設計理念：嚴格的電子郵件格式驗證
        /// </summary>
        public ValidationResult ValidateEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return new ValidationResult { IsValid = false, ErrorMessage = "電子郵件不能為空" };
            }

            // 電子郵件正則表達式
            var emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
            if (!Regex.IsMatch(email, emailPattern))
            {
                return new ValidationResult { IsValid = false, ErrorMessage = "電子郵件格式不正確" };
            }

            // 檢查長度
            if (email.Length > ApplicationConstants.Database.EmailMaxLength)
            {
                return new ValidationResult { IsValid = false, ErrorMessage = $"電子郵件長度超過限制 ({email.Length} > {ApplicationConstants.Database.EmailMaxLength})" };
            }

            return new ValidationResult { IsValid = true };
        }

        /// <summary>
        /// 驗證電話號碼
        /// 設計理念：支援多種電話號碼格式
        /// </summary>
        public ValidationResult ValidatePhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return new ValidationResult { IsValid = false, ErrorMessage = "電話號碼不能為空" };
            }

            // 移除所有非數字字元
            var cleanPhone = Regex.Replace(phone, @"[^\d]", "");

            // 檢查長度（台灣手機號碼 10 位，市話 8-9 位）
            if (cleanPhone.Length < 8 || cleanPhone.Length > 10)
            {
                return new ValidationResult { IsValid = false, ErrorMessage = "電話號碼長度不正確" };
            }

            return new ValidationResult { IsValid = true };
        }

        /// <summary>
        /// 驗證檔案名稱
        /// 設計理念：防止檔案名稱攻擊
        /// </summary>
        public ValidationResult ValidateFileName(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return new ValidationResult { IsValid = false, ErrorMessage = "檔案名稱不能為空" };
            }

            // 檢查危險字元
            if (DangerousFileNamePattern.IsMatch(fileName))
            {
                return new ValidationResult { IsValid = false, ErrorMessage = "檔案名稱包含危險字元" };
            }

            // 檢查長度
            if (fileName.Length > ApplicationConstants.Database.FileNameMaxLength)
            {
                return new ValidationResult { IsValid = false, ErrorMessage = $"檔案名稱長度超過限制 ({fileName.Length} > {ApplicationConstants.Database.FileNameMaxLength})" };
            }

            return new ValidationResult { IsValid = true };
        }

        /// <summary>
        /// 驗證檔案路徑
        /// 設計理念：防止路徑遍歷攻擊
        /// </summary>
        public ValidationResult ValidateFilePath(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return new ValidationResult { IsValid = false, ErrorMessage = "檔案路徑不能為空" };
            }

            // 檢查路徑遍歷
            if (PathTraversalPattern.IsMatch(filePath))
            {
                return new ValidationResult { IsValid = false, ErrorMessage = "檔案路徑包含危險內容" };
            }

            // 檢查長度
            if (filePath.Length > ApplicationConstants.Database.FilePathMaxLength)
            {
                return new ValidationResult { IsValid = false, ErrorMessage = $"檔案路徑長度超過限制 ({filePath.Length} > {ApplicationConstants.Database.FilePathMaxLength})" };
            }

            return new ValidationResult { IsValid = true };
        }

        /// <summary>
        /// 驗證 SQL 注入
        /// 設計理念：檢測 SQL 注入攻擊模式
        /// </summary>
        public ValidationResult ValidateSqlInjection(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return new ValidationResult { IsValid = true };
            }

            foreach (var pattern in SqlInjectionPatterns)
            {
                if (pattern.IsMatch(value))
                {
                    _logger.LogWarning("SQL 注入檢測 - 檢測到危險模式: {Pattern}", pattern.ToString());
                    return new ValidationResult { IsValid = false, ErrorMessage = "輸入包含危險內容" };
                }
            }

            return new ValidationResult { IsValid = true };
        }

        /// <summary>
        /// 驗證 XSS 攻擊
        /// 設計理念：檢測 XSS 攻擊模式
        /// </summary>
        public ValidationResult ValidateXss(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return new ValidationResult { IsValid = true };
            }

            foreach (var pattern in DangerousPatterns)
            {
                if (pattern.IsMatch(value))
                {
                    _logger.LogWarning("XSS 檢測 - 檢測到危險模式: {Pattern}", pattern.ToString());
                    return new ValidationResult { IsValid = false, ErrorMessage = "輸入包含危險內容" };
                }
            }

            return new ValidationResult { IsValid = true };
        }

        /// <summary>
        /// 驗證檔案類型
        /// 設計理念：檢查檔案副檔名安全性
        /// </summary>
        public ValidationResult ValidateFileType(string? fileName, string[] allowedExtensions)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return new ValidationResult { IsValid = false, ErrorMessage = "檔案名稱不能為空" };
            }

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                return new ValidationResult { IsValid = false, ErrorMessage = $"不支援的檔案類型: {extension}" };
            }

            return new ValidationResult { IsValid = true };
        }

        /// <summary>
        /// 驗證檔案大小
        /// 設計理念：防止檔案大小攻擊
        /// </summary>
        public ValidationResult ValidateFileSize(long fileSize, long maxSize)
        {
            if (fileSize <= 0)
            {
                return new ValidationResult { IsValid = false, ErrorMessage = "檔案大小必須大於 0" };
            }

            if (fileSize > maxSize)
            {
                return new ValidationResult { IsValid = false, ErrorMessage = $"檔案大小超過限制 ({fileSize} > {maxSize})" };
            }

            return new ValidationResult { IsValid = true };
        }

        /// <summary>
        /// 清理和正規化輸入
        /// 設計理念：清理危險字元，正規化輸入
        /// </summary>
        public string SanitizeInput(string? input, SanitizationOptions? options = null)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            var sanitized = input;

            // 移除 HTML 標籤
            if (options?.RemoveHtmlTags != false)
            {
                sanitized = Regex.Replace(sanitized, @"<[^>]*>", string.Empty);
            }

            // 移除危險字元
            if (options?.RemoveDangerousChars != false)
            {
                sanitized = Regex.Replace(sanitized, @"[<>""'&]", string.Empty);
            }

            // 正規化空白字元
            if (options?.NormalizeWhitespace != false)
            {
                sanitized = Regex.Replace(sanitized, @"\s+", " ").Trim();
            }

            // 限制長度
            if (options?.MaxLength.HasValue == true && sanitized.Length > options.MaxLength.Value)
            {
                sanitized = sanitized.Substring(0, options.MaxLength.Value);
            }

            return sanitized;
        }

        /// <summary>
        /// 驗證專案 ID
        /// 設計理念：專案 ID 格式驗證
        /// </summary>
        public ValidationResult ValidateProjectId(string? projectId)
        {
            if (string.IsNullOrWhiteSpace(projectId))
            {
                return new ValidationResult { IsValid = false, ErrorMessage = "專案 ID 不能為空" };
            }

            if (projectId.Length > ApplicationConstants.Database.ProjectIdMaxLength)
            {
                return new ValidationResult { IsValid = false, ErrorMessage = $"專案 ID 長度超過限制 ({projectId.Length} > {ApplicationConstants.Database.ProjectIdMaxLength})" };
            }

            // 檢查格式（只允許字母、數字、底線、連字號）
            if (!Regex.IsMatch(projectId, @"^[a-zA-Z0-9_-]+$"))
            {
                return new ValidationResult { IsValid = false, ErrorMessage = "專案 ID 格式不正確" };
            }

            return new ValidationResult { IsValid = true };
        }

        /// <summary>
        /// 驗證分頁參數
        /// 設計理念：分頁參數安全性驗證
        /// </summary>
        public ValidationResult ValidatePagination(int page, int pageSize)
        {
            if (page < 1)
            {
                return new ValidationResult { IsValid = false, ErrorMessage = "頁碼必須大於 0" };
            }

            if (pageSize < ApplicationConstants.Database.MinPageSize || pageSize > ApplicationConstants.Database.MaxPageSize)
            {
                return new ValidationResult { IsValid = false, ErrorMessage = $"頁面大小必須在 {ApplicationConstants.Database.MinPageSize} 到 {ApplicationConstants.Database.MaxPageSize} 之間" };
            }

            return new ValidationResult { IsValid = true };
        }

        /// <summary>
        /// 驗證搜尋關鍵字
        /// 設計理念：搜尋關鍵字安全性驗證
        /// </summary>
        public ValidationResult ValidateSearchKeyword(string? keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return new ValidationResult { IsValid = true };
            }

            // 檢查長度
            if (keyword.Length > 100)
            {
                return new ValidationResult { IsValid = false, ErrorMessage = "搜尋關鍵字長度超過限制" };
            }

            // 檢查 SQL 注入
            var sqlResult = ValidateSqlInjection(keyword);
            if (!sqlResult.IsValid)
            {
                return sqlResult;
            }

            // 檢查 XSS
            var xssResult = ValidateXss(keyword);
            if (!xssResult.IsValid)
            {
                return xssResult;
            }

            return new ValidationResult { IsValid = true };
        }
    }

    /// <summary>
    /// 驗證結果
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public object? Details { get; set; }
    }

    /// <summary>
    /// 驗證選項
    /// </summary>
    public class ValidationOptions
    {
        public bool AllowNull { get; set; } = false;
        public int? MinLength { get; set; }
        public int? MaxLength { get; set; }
        public string? Pattern { get; set; }
        public bool CheckSqlInjection { get; set; } = true;
        public bool CheckXss { get; set; } = true;
    }

    /// <summary>
    /// 清理選項
    /// </summary>
    public class SanitizationOptions
    {
        public bool RemoveHtmlTags { get; set; } = true;
        public bool RemoveDangerousChars { get; set; } = true;
        public bool NormalizeWhitespace { get; set; } = true;
        public int? MaxLength { get; set; }
    }
} 