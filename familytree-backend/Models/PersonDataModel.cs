// 人員資料模型 - 定義人員資料相關的資料結構
using System.ComponentModel.DataAnnotations;

namespace familytree_backend.Models
{
    public class PersonDataModel
    {
        public int Id { get; set; }
        
        [Required]
        public string FileMd5 { get; set; } = string.Empty;  // 檔案MD5(唯一PK)
        
        public string? Photo { get; set; }                   // 照片
        
        public string? PhotoIndex { get; set; }              // 照片索引
        
        [Required]
        public string Name { get; set; } = string.Empty;     // 姓名
        
        public string? DiscoveryProcess { get; set; }        // 發掘經過
        
        public string? Gender { get; set; }                  // 性別
        
        public string? Birthday { get; set; }                // 生日
        
        public string? Birthplace { get; set; }              // 出生地（父母戶籍所在地）
        
        public string? Nationality { get; set; }             // 國籍
        
        public string? Ethnicity { get; set; }               // 民族
        
        public string? AncestralHome { get; set; }           // 籍貫（祖父戶籍所在地）
        
        public string? PoliticalParty { get; set; }          // 黨派
        
        public string? IdNumber { get; set; }                // 身分證號碼
        
        public string? PassportNumber { get; set; }          // 護照號碼
        
        public string? Phone { get; set; }                   // 電話
        
        public string? Mobile { get; set; }                  // 行動電話
        
        public string? Email { get; set; }                   // 電子信箱
        
        public string? CurrentWorkplace { get; set; }        // 現職單位
        
        public string? CurrentAddress { get; set; }          // 現居地址
        
        public string? MailingAddress { get; set; }          // 通訊地址
        
        public string? FamilyRelationships { get; set; }     // 親屬關係（職稱，姓名）
        
        public string? CurrentEmployer { get; set; }         // 現職
        
        public string? Friends { get; set; }                 // 友人
        
        public string? Experience { get; set; }              // 經歷（單位，職稱，任職期間）
        
        public string? Education { get; set; }               // 學歷
        
        public string? OnlineAccounts { get; set; }          // 網路帳號
        
        public string? Publications { get; set; }            // 著作（名稱，共同作者）
        
        public string? Activities { get; set; }              // 參與活動（活動名稱，參與人士）
        
        public string? ImportantFriends { get; set; }        // 重要友人（姓名，單位，關聯事件）
        
        public string? FrequentPlaces { get; set; }          // 經常出入場所
        
        public string? TravelRecords { get; set; }           // 出國紀錄
        
        public string? TravelHistory { get; set; }           // 出國經歷
        
        public string? FrequentLocations { get; set; }       // 經常出入地點
        
        public string? Address { get; set; }                 // 地址
        
        public string? AncestralOrigin { get; set; }         // 祖籍
        
        public string? ExtraData { get; set; }               // 額外資料
        
        public string? Remarks { get; set; }                 // 備註
        
        public string? Notes { get; set; }                   // 筆記
        
        public string? ProjectId { get; set; }              // 專案ID
        
        public string? UserId { get; set; }                 // 使用者ID
        
        public DateTime CreatedAt { get; set; }              // 建檔時間
        
        public string? CreatedBy { get; set; }               // 建檔人
        
        public DateTime UpdatedAt { get; set; }              // 最後更新時間
        
        public string? UpdatedBy { get; set; }               // 最後更新人
    }



    public class PersonDataRequest
    {
        public string? Photo { get; set; }
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public string? DiscoveryProcess { get; set; }
        public string? Gender { get; set; }
        public string? Birthday { get; set; }
        public string? Birthplace { get; set; }
        public string? Nationality { get; set; }
        public string? Ethnicity { get; set; }
        public string? AncestralHome { get; set; }
        public string? PoliticalParty { get; set; }
        public string? IdNumber { get; set; }
        public string? PassportNumber { get; set; }
        public string? Phone { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public string? CurrentWorkplace { get; set; }
        public string? CurrentAddress { get; set; }
        public string? MailingAddress { get; set; }
        public string? FamilyRelationships { get; set; }
        public string? Experience { get; set; }
        public string? Education { get; set; }
        public string? OnlineAccounts { get; set; }
        public string? Publications { get; set; }
        public string? Activities { get; set; }
        public string? ImportantFriends { get; set; }
        public string? FrequentPlaces { get; set; }
        public string? TravelRecords { get; set; }
        public string? Notes { get; set; }
    }
} 