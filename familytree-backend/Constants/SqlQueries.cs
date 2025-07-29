// SQL 查詢常數管理 - 集中管理所有 SQL 查詢語句
namespace familytree_backend.Constants
{
    /// <summary>
    /// SQL 查詢常數
    /// 設計理念：將所有 SQL 查詢語句集中管理，便於維護和優化
    /// 職責：提供統一的 SQL 查詢定義，避免硬編碼和重複
    /// </summary>
    public static class SqlQueries
    {
        /// <summary>
        /// 人員資料相關查詢
        /// </summary>
        public static class PersonProfile
        {
            /// <summary>
            /// 基本查詢欄位（用於列表查詢）
            /// </summary>
            public const string SelectFields = @"
                id, file_md5, photo_index as photo, name, discovery_source as discovery_process, 
                gender, birthday, birthplace, nationality, ethnicity, ancestral_origin as ancestral_home, 
                political_party, id_number, passport_number, phone, mobile, email, 
                current_employer as current_workplace, address as current_address, 
                mailing_address, experience, education, online_accounts, publications, 
                activities, frequent_locations as frequent_places, travel_history as travel_records, 
                family_relationships, friends as important_friends, remarks as notes, 
                extra_data as profiledata, created_at, updated_at, project_id";

            /// <summary>
            /// 根據 ID 查詢人員資料
            /// </summary>
            public const string SelectById = @"
                SELECT " + SelectFields + @"
                FROM person_profile 
                WHERE id = @id AND project_id = @project_id";

            /// <summary>
            /// 檢查人員資料是否存在
            /// </summary>
            public const string ExistsById = @"
                SELECT COUNT(*) FROM person_profile 
                WHERE id = @id AND project_id = @project_id";

            /// <summary>
            /// 插入人員資料
            /// </summary>
            public const string Insert = @"
                INSERT INTO person_profile (
                    file_md5, photo_index, name, discovery_source, gender, birthday, birthplace,
                    nationality, ethnicity, ancestral_origin, political_party, id_number, passport_number,
                    phone, mobile, email, current_employer, address, mailing_address,
                    family_relationships, experience, education, online_accounts, publications,
                    activities, friends, frequent_locations, travel_history, remarks,
                    project_id, created_at, updated_at
                ) VALUES (
                    @FileMd5, @Photo, @Name, @DiscoveryProcess, @Gender, @Birthday, @Birthplace,
                    @Nationality, @Ethnicity, @AncestralHome, @PoliticalParty, @IdNumber, @PassportNumber,
                    @Phone, @Mobile, @Email, @CurrentWorkplace, @CurrentAddress, @MailingAddress,
                    @FamilyRelationships, @Experience, @Education, @OnlineAccounts, @Publications,
                    @Activities, @ImportantFriends, @FrequentPlaces, @TravelRecords, @Notes,
                    @ProjectId, @CreatedAt, @UpdatedAt
                ) RETURNING id";

            /// <summary>
            /// 更新人員資料
            /// </summary>
            public const string Update = @"
                UPDATE person_profile SET 
                    file_md5 = @FileMd5, photo_index = @Photo, name = @Name, discovery_source = @DiscoveryProcess,
                    gender = @Gender, birthday = @Birthday, birthplace = @Birthplace, nationality = @Nationality,
                    ethnicity = @Ethnicity, ancestral_origin = @AncestralHome, political_party = @PoliticalParty,
                    id_number = @IdNumber, passport_number = @PassportNumber, phone = @Phone, mobile = @Mobile,
                    email = @Email, current_employer = @CurrentWorkplace, address = @CurrentAddress,
                    mailing_address = @MailingAddress, family_relationships = @FamilyRelationships,
                    experience = @Experience, education = @Education, online_accounts = @OnlineAccounts,
                    publications = @Publications, activities = @Activities, friends = @ImportantFriends,
                    frequent_locations = @FrequentPlaces, travel_history = @TravelRecords, remarks = @Notes,
                    updated_at = @UpdatedAt
                WHERE id = @Id AND project_id = @ProjectId";

            /// <summary>
            /// 刪除人員資料
            /// </summary>
            public const string Delete = @"
                DELETE FROM person_profile 
                WHERE id = @id AND project_id = @project_id";

            /// <summary>
            /// 檢查檔案是否已存在
            /// </summary>
            public const string FileExists = @"
                SELECT COUNT(*) FROM person_profile 
                WHERE file_md5 = @md5";
        }

        /// <summary>
        /// 專案管理相關查詢
        /// </summary>
        public static class Projects
        {
            /// <summary>
            /// 查詢專案列表（包含統計資料）
            /// </summary>
            public const string SelectListWithStats = @"
                SELECT 
                    p.id as Id, p.user_id as UserId, p.project_name as ProjectName, 
                    p.project_description as ProjectDescription, p.status as Status,
                    p.created_at as CreatedAt, p.completed_at as CompletedAt, p.updated_at as UpdatedAt,
                    (SELECT COUNT(*) FROM person_profile WHERE project_id = p.id) as MemberCount,
                    (SELECT COUNT(*) FROM relationship_layers WHERE project_id = p.id) as RelationshipCount
                FROM projects p
                WHERE p.user_id = @userId AND p.status != 'deleted'
                ORDER BY p.updated_at DESC";

            /// <summary>
            /// 插入新專案
            /// </summary>
            public const string Insert = @"
                INSERT INTO projects (id, user_id, project_name, project_description, status, created_at, updated_at)
                VALUES (@id, @userId, @projectName, @projectDescription, @status, @now, @now)";

            /// <summary>
            /// 更新專案
            /// </summary>
            public const string Update = @"
                UPDATE projects 
                SET project_name = @projectName, 
                    project_description = @projectDescription, 
                    status = @status, 
                    updated_at = @now
                WHERE id = @projectId";

            /// <summary>
            /// 軟刪除專案
            /// </summary>
            public const string SoftDelete = @"
                UPDATE projects 
                SET status = 'deleted', updated_at = @now 
                WHERE id = @projectId";
        }

        /// <summary>
        /// 全文檢索相關查詢
        /// </summary>
        public static class Search
        {
            /// <summary>
            /// 搜索結果欄位
            /// </summary>
            public const string SearchResultFields = @"
                id, name, gender, birthday, nationality, mobile, phone, 
                id_number, passport_number, family_relationships, friends,
                current_employer, education, extra_data as profiledata,
                created_at, updated_at, discovery_source as source";

            /// <summary>
            /// 精確搜索條件
            /// </summary>
            public const string ExactSearchCondition = @"
                (name = @keyword OR 
                 mobile = @keyword OR 
                 phone = @keyword OR 
                 id_number = @keyword OR 
                 passport_number = @keyword)";

            /// <summary>
            /// 模糊搜索條件
            /// </summary>
            public const string FuzzySearchCondition = @"
                (name ILIKE @keyword OR 
                 mobile ILIKE @keyword OR 
                 phone ILIKE @keyword OR 
                 id_number ILIKE @keyword OR 
                 passport_number ILIKE @keyword OR
                 family_relationships ILIKE @keyword OR
                 friends ILIKE @keyword OR
                 current_employer ILIKE @keyword OR
                 education ILIKE @keyword)";

            /// <summary>
            /// 記錄搜索關鍵字
            /// </summary>
            public const string RecordKeyword = @"
                INSERT INTO search_keywords (keyword, search_type, project_id, search_count, last_search_time)
                VALUES (@keyword, @searchType, @projectId, 1, @now)
                ON CONFLICT (keyword, search_type, project_id) 
                DO UPDATE SET 
                    search_count = search_keywords.search_count + 1,
                    last_search_time = @now";

            /// <summary>
            /// 獲取熱門關鍵字
            /// </summary>
            public const string GetPopularKeywords = @"
                SELECT keyword 
                FROM search_keywords 
                WHERE (@projectId IS NULL OR project_id = @projectId)
                ORDER BY search_count DESC, last_search_time DESC 
                LIMIT @limit";

            /// <summary>
            /// 獲取搜索歷史
            /// </summary>
            public const string GetSearchHistory = @"
                SELECT DISTINCT keyword 
                FROM search_keywords 
                WHERE (@projectId IS NULL OR project_id = @projectId)
                ORDER BY last_search_time DESC 
                LIMIT @limit";
        }

        /// <summary>
        /// 關係圖譜相關查詢
        /// </summary>
        public static class Relationships
        {
            /// <summary>
            /// 查詢人員關係
            /// </summary>
            public const string SelectByPerson = @"
                SELECT 
                    source_person_id as SourcePersonId,
                    target_person_id as TargetPersonId,
                    relation_type as RelationType,
                    source_field as SourceField,
                    project_id as ProjectId
                FROM relationship_layers 
                WHERE (source_person_id = @personId OR target_person_id = @personId)
                  AND project_id = @projectId";

            /// <summary>
            /// 插入關係資料
            /// </summary>
            public const string Insert = @"
                INSERT INTO relationship_layers (source_person_id, target_person_id, relation_type, source_field, project_id)
                VALUES (@SourcePersonId, @TargetPersonId, @RelationType, @SourceField, @ProjectId)";
        }

        /// <summary>
        /// 檔案上傳相關查詢
        /// </summary>
        public static class FileUploads
        {
            /// <summary>
            /// 記錄檔案上傳
            /// </summary>
            public const string RecordUpload = @"
                INSERT INTO file_uploads (file_name, md5_hash, file_size, status, project_id, upload_time)
                VALUES (@FileName, @Md5Hash, @FileSize, @Status, @ProjectId, @UploadTime)";
        }

        /// <summary>
        /// 統計查詢
        /// </summary>
        public static class Statistics
        {
            /// <summary>
            /// 獲取系統總統計
            /// </summary>
            public const string SystemStats = @"
                SELECT 
                    (SELECT COUNT(*) FROM projects WHERE status != 'deleted') as TotalProjects,
                    (SELECT COUNT(*) FROM person_profile WHERE project_id IN (SELECT id FROM projects WHERE status != 'deleted')) as TotalMembers,
                    (SELECT COUNT(*) FROM relationship_layers WHERE project_id IN (SELECT id FROM projects WHERE status != 'deleted')) as TotalRelationships,
                    (SELECT COUNT(*) FROM file_uploads) as TotalFileUploads";
        }
    }
} 