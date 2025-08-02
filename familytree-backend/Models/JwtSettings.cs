namespace familytree_backend.Models
{
    /// <summary>
    /// JWT 設定
    /// </summary>
    public class JwtSettings
    {
        public string Secret { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int AccessTokenExpiration { get; set; } = 15; // 分鐘
        public int RefreshTokenExpiration { get; set; } = 10080; // 分鐘 (7天)
    }
}