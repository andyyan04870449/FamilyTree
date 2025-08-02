namespace familytree_backend.Models
{
    /// <summary>
    /// JWT 密鑰輪換模型
    /// </summary>
    public class JwtKeyRotation
    {
        /// <summary>
        /// 當前密鑰 ID
        /// </summary>
        public string CurrentKeyId { get; set; } = string.Empty;

        /// <summary>
        /// 當前密鑰
        /// </summary>
        public string CurrentKey { get; set; } = string.Empty;

        /// <summary>
        /// 當前密鑰過期時間
        /// </summary>
        public DateTime CurrentKeyExpiry { get; set; }

        /// <summary>
        /// 下一個密鑰 ID（可選）
        /// </summary>
        public string? NextKeyId { get; set; }

        /// <summary>
        /// 下一個密鑰（可選）
        /// </summary>
        public string? NextKey { get; set; }

        /// <summary>
        /// 下一個密鑰啟用時間（可選）
        /// </summary>
        public DateTime? NextKeyActivation { get; set; }

        /// <summary>
        /// 密鑰版本號
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// 建立時間
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 最後更新時間
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 密鑰輪換配置
    /// </summary>
    public class KeyRotationSettings
    {
        /// <summary>
        /// 是否啟用自動密鑰輪換
        /// </summary>
        public bool EnableAutoRotation { get; set; } = true;

        /// <summary>
        /// 密鑰有效期（天）
        /// </summary>
        public int KeyValidityDays { get; set; } = 30;

        /// <summary>
        /// 提前輪換天數（在過期前多少天開始準備新密鑰）
        /// </summary>
        public int RotationAdvanceDays { get; set; } = 7;

        /// <summary>
        /// 舊密鑰保留期（天）- 允許舊 Token 在輪換後仍能驗證
        /// </summary>
        public int OldKeyRetentionDays { get; set; } = 2;

        /// <summary>
        /// 密鑰最小長度
        /// </summary>
        public int MinKeyLength { get; set; } = 64;

        /// <summary>
        /// 檢查輪換間隔（分鐘）
        /// </summary>
        public int CheckIntervalMinutes { get; set; } = 60;
    }
}