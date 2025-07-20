// 人員資料控制器 - 提供人員資料的CRUD操作和分頁查詢
using Microsoft.AspNetCore.Mvc;
using familytree_backend.Models;
using Dapper;
using Npgsql;

namespace familytree_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PersonDataController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly ILogger<PersonDataController> _logger;

        public PersonDataController(IConfiguration configuration, ILogger<PersonDataController> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetPersonDataList([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var offset = (page - 1) * pageSize;

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // 取得總數
                var countSql = "SELECT COUNT(*) FROM person_data";
                var totalCount = await connection.ExecuteScalarAsync<int>(countSql);

                // 取得分頁資料
                var sql = @"SELECT id, file_md5, photo, name, discovery_process, gender, birthday, 
                                  birthplace, nationality, ethnicity, ancestral_home, political_party, 
                                  id_number, passport_number, phone, mobile, email, current_workplace, 
                                  current_address, mailing_address, family_relationships, experience, 
                                  education, online_accounts, publications, activities, important_friends, 
                                  frequent_places, travel_records, notes, created_at, created_by, 
                                  updated_at, updated_by
                           FROM person_data 
                           ORDER BY created_at DESC 
                           LIMIT @pageSize OFFSET @offset";

                var parameters = new { pageSize, offset };
                var personDataList = await connection.QueryAsync<PersonDataModel>(sql, parameters);

                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var response = new PersonDataListResponse
                {
                    Success = true,
                    Message = "取得人員資料列表成功",
                    PersonDataList = personDataList.ToList(),
                    TotalCount = totalCount,
                    PageNumber = page,
                    PageSize = pageSize,
                    TotalPages = totalPages
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得人員資料列表失敗");
                return StatusCode(500, new { success = false, message = "取得人員資料列表失敗" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPersonData(int id)
        {
            try
            {
                _logger.LogInformation("=== 收到個人詳細資料請求 ===");
                _logger.LogInformation("請求參數: PersonId = {PersonId}", id);
                _logger.LogInformation("請求時間: {RequestTime}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                _logger.LogInformation("✅ 資料庫連線成功");

                var sql = @"SELECT id, file_md5, photo, name, discovery_process, gender, birthday, 
                                  birthplace, nationality, ethnicity, ancestral_home, political_party, 
                                  id_number, passport_number, phone, mobile, email, current_workplace, 
                                  current_address, mailing_address, family_relationships, experience, 
                                  education, online_accounts, publications, activities, important_friends, 
                                  frequent_places, travel_records, notes, created_at, created_by, 
                                  updated_at, updated_by
                           FROM person_data 
                           WHERE id = @id";

                _logger.LogInformation("🔍 執行資料庫查詢: PersonId = {PersonId}", id);
                var personData = await connection.QueryFirstOrDefaultAsync<PersonDataModel>(sql, new { id });

                if (personData == null)
                {
                    _logger.LogWarning("⚠️ 找不到人員資料: PersonId = {PersonId}", id);
                    return NotFound(new { success = false, message = "人員資料不存在" });
                }

                _logger.LogInformation("✅ 成功取得人員資料: PersonId = {PersonId}, Name = {Name}", id, personData.Name);
                _logger.LogInformation("📋 人員資料概覽:");
                _logger.LogInformation("  - 姓名: {Name}", personData.Name ?? "無");
                _logger.LogInformation("  - 性別: {Gender}", personData.Gender ?? "無");
                _logger.LogInformation("  - 生日: {Birthday}", personData.Birthday?.ToString("yyyy-MM-dd") ?? "無");
                _logger.LogInformation("  - 國籍: {Nationality}", personData.Nationality ?? "無");
                _logger.LogInformation("  - 電話: {Phone}", personData.Phone ?? "無");
                _logger.LogInformation("  - 手機: {Mobile}", personData.Mobile ?? "無");
                _logger.LogInformation("  - 信箱: {Email}", personData.Email ?? "無");
                _logger.LogInformation("  - 現居地址: {CurrentAddress}", personData.CurrentAddress ?? "無");
                _logger.LogInformation("  - 現職單位: {CurrentWorkplace}", personData.CurrentWorkplace ?? "無");
                _logger.LogInformation("  - 親屬關係: {FamilyRelationships}", personData.FamilyRelationships ?? "無");
                _logger.LogInformation("  - 重要友人: {ImportantFriends}", personData.ImportantFriends ?? "無");
                _logger.LogInformation("  - 工作經歷: {Experience}", personData.Experience ?? "無");
                _logger.LogInformation("  - 學歷: {Education}", personData.Education ?? "無");
                _logger.LogInformation("  - 網路帳號: {OnlineAccounts}", personData.OnlineAccounts ?? "無");
                _logger.LogInformation("  - 著作: {Publications}", personData.Publications ?? "無");
                _logger.LogInformation("  - 參與活動: {Activities}", personData.Activities ?? "無");
                _logger.LogInformation("  - 經常出入場所: {FrequentPlaces}", personData.FrequentPlaces ?? "無");
                _logger.LogInformation("  - 出國紀錄: {TravelRecords}", personData.TravelRecords ?? "無");
                _logger.LogInformation("  - 備註: {Notes}", personData.Notes ?? "無");

                var response = new PersonDataResponse
                {
                    Success = true,
                    Message = "取得人員資料成功",
                    PersonData = personData
                };

                _logger.LogInformation("✅ 回應建立成功，準備返回資料");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 取得人員資料失敗: PersonId = {PersonId}", id);
                _logger.LogError("錯誤詳情: {ErrorMessage}", ex.Message);
                _logger.LogError("堆疊追蹤: {StackTrace}", ex.StackTrace);
                return StatusCode(500, new { success = false, message = "取得人員資料失敗" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreatePersonData([FromBody] PersonDataRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest(new { success = false, message = "姓名為必填欄位" });
                }

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"INSERT INTO person_data (
                    file_md5, photo, name, discovery_process, gender, birthday, birthplace, 
                    nationality, ethnicity, ancestral_home, political_party, id_number, 
                    passport_number, phone, mobile, email, current_workplace, current_address, 
                    mailing_address, family_relationships, experience, education, online_accounts, 
                    publications, activities, important_friends, frequent_places, travel_records, 
                    notes, created_at, updated_at
                ) VALUES (
                    @fileMd5, @photo, @name, @discoveryProcess, @gender, @birthday, @birthplace,
                    @nationality, @ethnicity, @ancestralHome, @politicalParty, @idNumber,
                    @passportNumber, @phone, @mobile, @email, @currentWorkplace, @currentAddress,
                    @mailingAddress, @familyRelationships, @experience, @education, @onlineAccounts,
                    @publications, @activities, @importantFriends, @frequentPlaces, @travelRecords,
                    @notes, @createdAt, @updatedAt
                ) RETURNING id";

                var parameters = new
                {
                    fileMd5 = "", // 手動新增的資料沒有檔案MD5
                    request.Photo,
                    request.Name,
                    request.DiscoveryProcess,
                    request.Gender,
                    request.Birthday,
                    request.Birthplace,
                    request.Nationality,
                    request.Ethnicity,
                    request.AncestralHome,
                    request.PoliticalParty,
                    request.IdNumber,
                    request.PassportNumber,
                    request.Phone,
                    request.Mobile,
                    request.Email,
                    request.CurrentWorkplace,
                    request.CurrentAddress,
                    request.MailingAddress,
                    request.FamilyRelationships,
                    request.Experience,
                    request.Education,
                    request.OnlineAccounts,
                    request.Publications,
                    request.Activities,
                    request.ImportantFriends,
                    request.FrequentPlaces,
                    request.TravelRecords,
                    request.Notes,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var id = await connection.ExecuteScalarAsync<int>(sql, parameters);

                var response = new PersonDataResponse
                {
                    Success = true,
                    Message = "人員資料新增成功",
                    PersonData = new PersonDataModel
                    {
                        Id = id,
                        Name = request.Name,
                        // 其他欄位...
                    }
                };

                return CreatedAtAction(nameof(GetPersonData), new { id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "新增人員資料失敗");
                return StatusCode(500, new { success = false, message = "新增人員資料失敗" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePersonData(int id, [FromBody] PersonDataRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest(new { success = false, message = "姓名為必填欄位" });
                }

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"UPDATE person_data SET 
                    photo = @photo, name = @name, discovery_process = @discoveryProcess, 
                    gender = @gender, birthday = @birthday, birthplace = @birthplace,
                    nationality = @nationality, ethnicity = @ethnicity, ancestral_home = @ancestralHome, 
                    political_party = @politicalParty, id_number = @idNumber, passport_number = @passportNumber, 
                    phone = @phone, mobile = @mobile, email = @email, current_workplace = @currentWorkplace, 
                    current_address = @currentAddress, mailing_address = @mailingAddress, 
                    family_relationships = @familyRelationships, experience = @experience, 
                    education = @education, online_accounts = @onlineAccounts, publications = @publications, 
                    activities = @activities, important_friends = @importantFriends, 
                    frequent_places = @frequentPlaces, travel_records = @travelRecords, notes = @notes, 
                    updated_at = @updatedAt
                    WHERE id = @id";

                var parameters = new
                {
                    id,
                    request.Photo,
                    request.Name,
                    request.DiscoveryProcess,
                    request.Gender,
                    request.Birthday,
                    request.Birthplace,
                    request.Nationality,
                    request.Ethnicity,
                    request.AncestralHome,
                    request.PoliticalParty,
                    request.IdNumber,
                    request.PassportNumber,
                    request.Phone,
                    request.Mobile,
                    request.Email,
                    request.CurrentWorkplace,
                    request.CurrentAddress,
                    request.MailingAddress,
                    request.FamilyRelationships,
                    request.Experience,
                    request.Education,
                    request.OnlineAccounts,
                    request.Publications,
                    request.Activities,
                    request.ImportantFriends,
                    request.FrequentPlaces,
                    request.TravelRecords,
                    request.Notes,
                    UpdatedAt = DateTime.UtcNow
                };

                var rowsAffected = await connection.ExecuteAsync(sql, parameters);

                if (rowsAffected == 0)
                {
                    return NotFound(new { success = false, message = "人員資料不存在" });
                }

                var response = new PersonDataResponse
                {
                    Success = true,
                    Message = "人員資料更新成功"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新人員資料失敗: {Id}", id);
                return StatusCode(500, new { success = false, message = "更新人員資料失敗" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePersonData(int id)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var rowsAffected = await connection.ExecuteAsync(
                    "DELETE FROM person_data WHERE id = @id", new { id });

                if (rowsAffected == 0)
                {
                    return NotFound(new { success = false, message = "人員資料不存在" });
                }

                var response = new PersonDataResponse
                {
                    Success = true,
                    Message = "人員資料刪除成功"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刪除人員資料失敗: {Id}", id);
                return StatusCode(500, new { success = false, message = "刪除人員資料失敗" });
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchPersonData([FromQuery] string? name, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var offset = (page - 1) * pageSize;

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                string whereClause = "";
                object parameters;

                if (!string.IsNullOrWhiteSpace(name))
                {
                    whereClause = "WHERE name ILIKE @name";
                    parameters = new { name = $"%{name}%", pageSize, offset };
                }
                else
                {
                    parameters = new { pageSize, offset };
                }

                // 取得總數
                var countSql = $"SELECT COUNT(*) FROM person_data {whereClause}";
                var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

                // 取得分頁資料
                var sql = $@"SELECT id, file_md5, photo, name, discovery_process, gender, birthday, 
                                   birthplace, nationality, ethnicity, ancestral_home, political_party, 
                                   id_number, passport_number, phone, mobile, email, current_workplace, 
                                   current_address, mailing_address, family_relationships, experience, 
                                   education, online_accounts, publications, activities, important_friends, 
                                   frequent_places, travel_records, notes, created_at, created_by, 
                                   updated_at, updated_by
                            FROM person_data 
                            {whereClause}
                            ORDER BY created_at DESC 
                            LIMIT @pageSize OFFSET @offset";

                var personDataList = await connection.QueryAsync<PersonDataModel>(sql, parameters);

                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var response = new PersonDataListResponse
                {
                    Success = true,
                    Message = "搜尋人員資料成功",
                    PersonDataList = personDataList.ToList(),
                    TotalCount = totalCount,
                    PageNumber = page,
                    PageSize = pageSize,
                    TotalPages = totalPages
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜尋人員資料失敗");
                return StatusCode(500, new { success = false, message = "搜尋人員資料失敗" });
            }
        }
    }
} 