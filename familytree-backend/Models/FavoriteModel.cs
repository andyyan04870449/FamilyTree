namespace familytree_backend.Models
{
    /// <summary>
    /// 我的最愛模型
    /// </summary>
    public class FavoriteModel
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int PersonId { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // 關聯的人員資訊
        public string? PersonName { get; set; }
        public string? PersonGender { get; set; }
        public string? PersonEmail { get; set; }
        public string? PersonMobile { get; set; }
    }
}