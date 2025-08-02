using System.Net;

namespace familytree_backend.Models.Exceptions
{
    /// <summary>
    /// 技術性異常
    /// 用於處理系統層面的錯誤，如資料庫連接、外部服務調用等
    /// </summary>
    public class TechnicalException : BaseApplicationException
    {
        public TechnicalException(string message, Exception? innerException = null, object? details = null)
            : base(message, HttpStatusCode.InternalServerError, "TECHNICAL_ERROR", details, shouldLog: true, innerException)
        {
        }
    }

    /// <summary>
    /// 資料庫操作異常
    /// </summary>
    public class DatabaseException : BaseApplicationException
    {
        public DatabaseException(string message, Exception? innerException = null, object? details = null)
            : base(message, HttpStatusCode.InternalServerError, "DATABASE_ERROR", details, shouldLog: true, innerException)
        {
        }

        public static DatabaseException ConnectionFailed(Exception? innerException = null)
        {
            return new DatabaseException("資料庫連接失敗", innerException);
        }

        public static DatabaseException QueryFailed(string operation, Exception? innerException = null)
        {
            return new DatabaseException($"資料庫查詢失敗: {operation}", innerException, new { Operation = operation });
        }

        public static DatabaseException TransactionFailed(Exception? innerException = null)
        {
            return new DatabaseException("資料庫交易失敗", innerException);
        }
    }

    /// <summary>
    /// 外部服務異常
    /// </summary>
    public class ExternalServiceException : BaseApplicationException
    {
        public ExternalServiceException(string serviceName, string message, Exception? innerException = null)
            : base($"外部服務錯誤 ({serviceName}): {message}", HttpStatusCode.BadGateway, "EXTERNAL_SERVICE_ERROR", 
                  new { ServiceName = serviceName }, shouldLog: true, innerException)
        {
        }

        public static ExternalServiceException ServiceUnavailable(string serviceName)
        {
            return new ExternalServiceException(serviceName, "服務不可用");
        }

        public static ExternalServiceException Timeout(string serviceName)
        {
            return new ExternalServiceException(serviceName, "服務回應逾時");
        }
    }

    /// <summary>
    /// 檔案操作異常
    /// </summary>
    public class FileOperationException : BaseApplicationException
    {
        public FileOperationException(string message, string? fileName = null, Exception? innerException = null)
            : base(message, HttpStatusCode.InternalServerError, "FILE_OPERATION_ERROR", 
                  new { FileName = fileName }, shouldLog: true, innerException)
        {
        }

        public static FileOperationException FileNotFound(string fileName)
        {
            return new FileOperationException($"檔案不存在: {fileName}", fileName);
        }

        public static FileOperationException UploadFailed(string fileName, Exception? innerException = null)
        {
            return new FileOperationException($"檔案上傳失敗: {fileName}", fileName, innerException);
        }

        public static FileOperationException InvalidFileType(string fileName, string[] allowedTypes)
        {
            return new FileOperationException(
                $"不支援的檔案類型: {fileName}，支援的類型: {string.Join(", ", allowedTypes)}", 
                fileName);
        }

        public static FileOperationException FileSizeExceeded(string fileName, long maxSize)
        {
            return new FileOperationException(
                $"檔案大小超過限制: {fileName}，最大允許: {maxSize} bytes", 
                fileName);
        }
    }

    /// <summary>
    /// 設定異常
    /// </summary>
    public class ConfigurationException : BaseApplicationException
    {
        public ConfigurationException(string message, string? configKey = null)
            : base(message, HttpStatusCode.InternalServerError, "CONFIGURATION_ERROR", 
                  new { ConfigurationKey = configKey }, shouldLog: true)
        {
        }

        public static ConfigurationException MissingConfiguration(string configKey)
        {
            return new ConfigurationException($"缺少必要的配置項: {configKey}", configKey);
        }

        public static ConfigurationException InvalidConfiguration(string configKey, string expectedFormat)
        {
            return new ConfigurationException($"配置項格式錯誤: {configKey}，期望格式: {expectedFormat}", configKey);
        }
    }
}