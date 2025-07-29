// 收藏功能控制器：提供用戶收藏管理功能
// 主要功能：添加收藏、刪除收藏、獲取收藏列表、更新查看時間

using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Dapper;
using familytree_backend.Models;
using familytree_backend.Services;

namespace familytree_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FavoritesController : BaseController
    {
        private readonly string _connectionString;

        public FavoritesController(IConfiguration configuration, ILogger<FavoritesController> logger, IConfigurationService configurationService,
            IValidationService validationService,
            IAccessControlService accessControlService,
            ILoggingService loggingService)
            : base(logger, configurationService, validationService, accessControlService, loggingService)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("找不到資料庫連接字符串");
        }

        /// <summary>
        /// 獲取用戶收藏列表
        /// </summary>
        /// <param name="project_id">專案ID</param>
        /// <returns>收藏列表</returns>
        [HttpGet]
        public async Task<IActionResult> GetFavorites([FromQuery] string? project_id = null)
        {
            Logger.LogInformation("📋 獲取用戶收藏列表請求，專案ID: {projectId}", project_id);

            try
            {
                // 驗證專案 ID
                var projectValidationResult = ValidateProjectId(project_id, allowNull: false);
                if (projectValidationResult != null)
                {
                    return projectValidationResult;
                }

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT f.id as Id, 
                           f.person_id as PersonId, 
                           f.person_name as PersonName, 
                           f.last_viewed_time as LastViewedTime, 
                           f.favorited_at as FavoritedAt,
                           f.created_at as CreatedAt, 
                           f.updated_at as UpdatedAt,
                           f.project_id as ProjectId
                    FROM user_favorites f
                    WHERE f.project_id = @projectId
                    ORDER BY 
                        CASE WHEN f.last_viewed_time IS NOT NULL THEN f.last_viewed_time ELSE f.favorited_at END DESC";

                var favorites = await connection.QueryAsync<UserFavorite>(sql, new { projectId = project_id });
                var favoriteList = favorites.ToList();

                // 轉換為前端顯示格式
                var favoriteItems = favoriteList.Select(f => new FavoriteItem
                {
                    Id = f.Id,
                    PersonId = f.PersonId,
                    PersonName = f.PersonName,
                    LastViewedTime = f.LastViewedTime,
                    FavoritedAt = f.FavoritedAt,
                    DisplayTime = GetDisplayTime(f.LastViewedTime, f.FavoritedAt),
                    CanDelete = true
                }).ToList();

                Logger.LogInformation("✅ 成功獲取收藏列表: 數量={count}，專案ID: {projectId}", favoriteItems.Count, project_id);

                return Ok(new FavoriteListResult
                {
                    Success = true,
                    Message = $"獲取收藏列表成功，共 {favoriteItems.Count} 筆",
                    Data = favoriteItems
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "❌ 獲取收藏列表失敗: {error}，專案ID: {projectId}", ex.Message, project_id);
                return StatusCode(500, new FavoriteListResult
                {
                    Success = false,
                    Message = "獲取收藏列表時發生錯誤",
                    Data = new List<FavoriteItem>()
                });
            }
        }

        /// <summary>
        /// 添加收藏
        /// </summary>
        /// <param name="request">收藏請求</param>
        /// <param name="project_id">專案ID</param>
        /// <returns>操作結果</returns>
        [HttpPost]
        public async Task<IActionResult> AddFavorite([FromBody] FavoriteRequest request, [FromQuery] string? project_id = null)
        {
            Logger.LogInformation("📋 添加收藏請求: PersonId={personId}, PersonName='{personName}', 專案ID: {projectId}", 
                request.PersonId, request.PersonName, project_id);

            try
            {
                // 驗證專案 ID
                var projectValidationResult = ValidateProjectId(project_id, allowNull: false);
                if (projectValidationResult != null)
                {
                    return projectValidationResult;
                }

                // 參數驗證
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    var errorMessage = string.Join("; ", errors);
                    Logger.LogWarning("⚠️  請求參數驗證失敗: {errors}", errorMessage);
                    return BadRequest(new FavoriteResult
                    {
                        Success = false,
                        Message = $"參數驗證失敗: {errorMessage}"
                    });
                }

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // 檢查是否已經收藏（同專案內）
                var existingFavorite = await connection.QueryFirstOrDefaultAsync<int?>(
                    "SELECT id FROM user_favorites WHERE person_id = @personId AND project_id = @projectId",
                    new { personId = request.PersonId, projectId = project_id });

                if (existingFavorite.HasValue)
                {
                    Logger.LogInformation("⚠️  人員已經收藏: PersonId={personId}, 專案ID: {projectId}", request.PersonId, project_id);
                    return Conflict(new FavoriteResult
                    {
                        Success = false,
                        Message = $"該人員 '{request.PersonName}' 已經收藏過了"
                    });
                }

                // 驗證人員是否存在於該專案
                var personExists = await connection.QueryFirstOrDefaultAsync<bool>(
                    "SELECT COUNT(*) > 0 FROM person_profile WHERE id = @personId AND project_id = @projectId",
                    new { personId = request.PersonId, projectId = project_id });

                if (!personExists)
                {
                    Logger.LogWarning("⚠️  人員不存在於該專案: PersonId={personId}, 專案ID: {projectId}", request.PersonId, project_id);
                    return NotFound(new FavoriteResult
                    {
                        Success = false,
                        Message = $"在該專案中找不到 ID 為 {request.PersonId} 的人員"
                    });
                }

                // 添加收藏
                var now = DateTime.UtcNow;
                var sql = @"
                    INSERT INTO user_favorites (person_id, person_name, project_id, favorited_at, created_at, updated_at)
                    VALUES (@personId, @personName, @projectId, @now, @now, @now)
                    RETURNING id";

                var favoriteId = await connection.QuerySingleAsync<int>(sql, new
                {
                    personId = request.PersonId,
                    personName = request.PersonName,
                    projectId = project_id,
                    now
                });

                Logger.LogInformation("✅ 收藏添加成功: FavoriteId={favoriteId}, PersonId={personId}, PersonName='{personName}', 專案ID: {projectId}", 
                    favoriteId, request.PersonId, request.PersonName, project_id);

                return Ok(new FavoriteResult
                {
                    Success = true,
                    Message = $"成功收藏 '{request.PersonName}'",
                    Data = new FavoriteData
                    {
                        Id = favoriteId,
                        PersonId = request.PersonId,
                        PersonName = request.PersonName,
                        FavoritedAt = now
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "❌ 添加收藏失敗: PersonId={personId}, PersonName='{personName}', 專案ID: {projectId}, 錯誤={error}", 
                    request.PersonId, request.PersonName, project_id, ex.Message);
                return StatusCode(500, new FavoriteResult
                {
                    Success = false,
                    Message = "添加收藏時發生錯誤，請稍後再試"
                });
            }
        }

        /// <summary>
        /// 刪除收藏
        /// </summary>
        /// <param name="id">收藏ID</param>
        /// <param name="project_id">專案ID</param>
        /// <returns>操作結果</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoveFavorite(int id, [FromQuery] string? project_id = null)
        {
            Logger.LogInformation("📋 刪除收藏請求: FavoriteId={favoriteId}, 專案ID: {projectId}", id, project_id);

            try
            {
                // 驗證專案 ID
                var projectValidationResult = ValidateProjectId(project_id, allowNull: false);
                if (projectValidationResult != null)
                {
                    return projectValidationResult;
                }

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // 獲取收藏資訊（確保是同專案的收藏）
                var favorite = await connection.QueryFirstOrDefaultAsync<UserFavorite>(
                    "SELECT id, person_id, person_name, favorited_at, project_id FROM user_favorites WHERE id = @id AND project_id = @projectId",
                    new { id, projectId = project_id });

                if (favorite == null)
                {
                    Logger.LogWarning("⚠️  收藏不存在或不屬於該專案: FavoriteId={favoriteId}, 專案ID: {projectId}", id, project_id);
                    return NotFound(new FavoriteResult
                    {
                        Success = false,
                        Message = $"找不到 ID 為 {id} 的收藏或該收藏不屬於當前專案"
                    });
                }

                // 刪除收藏
                var deletedRows = await connection.ExecuteAsync(
                    "DELETE FROM user_favorites WHERE id = @id AND project_id = @projectId",
                    new { id, projectId = project_id });

                if (deletedRows > 0)
                {
                    Logger.LogInformation("✅ 收藏刪除成功: FavoriteId={favoriteId}, PersonName='{personName}', 專案ID: {projectId}", 
                        id, favorite.PersonName, project_id);

                    return Ok(new FavoriteResult
                    {
                        Success = true,
                        Message = $"成功取消收藏 '{favorite.PersonName}'"
                    });
                }
                else
                {
                    Logger.LogWarning("⚠️  收藏刪除失敗，沒有刪除任何記錄: FavoriteId={favoriteId}, 專案ID: {projectId}", id, project_id);
                    return BadRequest(new FavoriteResult
                    {
                        Success = false,
                        Message = "刪除收藏失敗"
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "❌ 刪除收藏失敗: FavoriteId={favoriteId}, 專案ID: {projectId}, 錯誤={error}", id, project_id, ex.Message);
                return StatusCode(500, new FavoriteResult
                {
                    Success = false,
                    Message = "刪除收藏時發生錯誤，請稍後再試"
                });
            }
        }

        /// <summary>
        /// 通過人員ID刪除收藏
        /// </summary>
        /// <param name="personId">人員ID</param>
        /// <param name="project_id">專案ID</param>
        /// <returns>操作結果</returns>
        [HttpDelete("person/{personId}")]
        public async Task<IActionResult> RemoveFavoriteByPersonId(int personId, [FromQuery] string? project_id = null)
        {
            Logger.LogInformation("📋 通過人員ID刪除收藏請求: PersonId={personId}, 專案ID: {projectId}", personId, project_id);

            try
            {
                // 驗證專案 ID
                var projectValidationResult = ValidateProjectId(project_id, allowNull: false);
                if (projectValidationResult != null)
                {
                    return projectValidationResult;
                }

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // 獲取收藏資訊（確保是同專案的收藏）
                var favorite = await connection.QueryFirstOrDefaultAsync<UserFavorite>(
                    "SELECT id, person_id, person_name, favorited_at, project_id FROM user_favorites WHERE person_id = @personId AND project_id = @projectId",
                    new { personId, projectId = project_id });

                if (favorite == null)
                {
                    Logger.LogWarning("⚠️  該人員未被收藏或不屬於該專案: PersonId={personId}, 專案ID: {projectId}", personId, project_id);
                    return NotFound(new FavoriteResult
                    {
                        Success = false,
                        Message = $"該人員未被收藏或不屬於當前專案"
                    });
                }

                // 刪除收藏
                var deletedRows = await connection.ExecuteAsync(
                    "DELETE FROM user_favorites WHERE person_id = @personId AND project_id = @projectId",
                    new { personId, projectId = project_id });

                if (deletedRows > 0)
                {
                    Logger.LogInformation("✅ 通過人員ID刪除收藏成功: PersonId={personId}, PersonName='{personName}', 專案ID: {projectId}", 
                        personId, favorite.PersonName, project_id);

                    return Ok(new FavoriteResult
                    {
                        Success = true,
                        Message = $"成功取消收藏 '{favorite.PersonName}'"
                    });
                }
                else
                {
                    Logger.LogWarning("⚠️  通過人員ID刪除收藏失敗: PersonId={personId}, 專案ID: {projectId}", personId, project_id);
                    return BadRequest(new FavoriteResult
                    {
                        Success = false,
                        Message = "取消收藏失敗"
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "❌ 通過人員ID刪除收藏失敗: PersonId={personId}, 專案ID: {projectId}, 錯誤={error}", personId, project_id, ex.Message);
                return StatusCode(500, new FavoriteResult
                {
                    Success = false,
                    Message = "取消收藏時發生錯誤，請稍後再試"
                });
            }
        }

        /// <summary>
        /// 檢查是否已收藏
        /// </summary>
        /// <param name="personId">人員ID</param>
        /// <param name="project_id">專案ID</param>
        /// <returns>是否已收藏</returns>
        [HttpGet("check/{personId}")]
        public async Task<IActionResult> CheckFavoriteStatus(int personId, [FromQuery] string? project_id = null)
        {
            Logger.LogInformation("📋 檢查收藏狀態請求: PersonId={personId}, 專案ID: {projectId}", personId, project_id);

            try
            {
                // 驗證專案 ID
                var projectValidationResult = ValidateProjectId(project_id, allowNull: false);
                if (projectValidationResult != null)
                {
                    return projectValidationResult;
                }

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var favoriteId = await connection.QueryFirstOrDefaultAsync<int?>(
                    "SELECT id FROM user_favorites WHERE person_id = @personId AND project_id = @projectId",
                    new { personId, projectId = project_id });

                var isFavorited = favoriteId.HasValue;

                Logger.LogInformation("✅ 收藏狀態檢查完成: PersonId={personId}, 專案ID: {projectId}, IsFavorited={isFavorited}", 
                    personId, project_id, isFavorited);

                return Ok(ApiResponse<object>.SuccessResult(new
                {
                    personId,
                    isFavorited,
                    favoriteId = favoriteId
                }, "檢查收藏狀態成功"));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "❌ 檢查收藏狀態失敗: PersonId={personId}, 專案ID: {projectId}, 錯誤={error}", personId, project_id, ex.Message);
                return StatusCode(500, ApiResponse<object>.ErrorResult("檢查收藏狀態時發生錯誤"));
            }
        }

        /// <summary>
        /// 更新收藏的查看時間
        /// </summary>
        /// <param name="personId">人員ID</param>
        /// <param name="project_id">專案ID</param>
        /// <returns>操作結果</returns>
        [HttpPut("view/{personId}")]
        public async Task<IActionResult> UpdateViewTime(int personId, [FromQuery] string? project_id = null)
        {
            Logger.LogInformation("📋 更新收藏查看時間請求: PersonId={personId}, 專案ID: {projectId}", personId, project_id);

            try
            {
                // 驗證專案 ID
                var projectValidationResult = ValidateProjectId(project_id, allowNull: false);
                if (projectValidationResult != null)
                {
                    return projectValidationResult;
                }

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var now = DateTime.UtcNow;
                var updatedRows = await connection.ExecuteAsync(@"
                    UPDATE user_favorites 
                    SET last_viewed_time = @now, updated_at = @now
                    WHERE person_id = @personId AND project_id = @projectId",
                    new { personId, projectId = project_id, now });

                if (updatedRows > 0)
                {
                    Logger.LogInformation("✅ 收藏查看時間更新成功: PersonId={personId}, 專案ID: {projectId}", personId, project_id);

                    return Ok(ApiResponse<object>.SuccessResult(new
                    {
                        personId,
                        lastViewedTime = now
                    }, "查看時間更新成功"));
                }
                else
                {
                    Logger.LogWarning("⚠️  該人員未被收藏或不屬於該專案，無法更新查看時間: PersonId={personId}, 專案ID: {projectId}", personId, project_id);
                    return NotFound(ApiResponse<object>.ErrorResult("該人員未被收藏或不屬於當前專案"));
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "❌ 更新收藏查看時間失敗: PersonId={personId}, 專案ID: {projectId}, 錯誤={error}", personId, project_id, ex.Message);
                return StatusCode(500, ApiResponse<object>.ErrorResult("更新查看時間時發生錯誤"));
            }
        }

        /// <summary>
        /// 清空所有收藏
        /// </summary>
        /// <param name="project_id">專案ID</param>
        /// <returns>操作結果</returns>
        [HttpDelete("clear")]
        public async Task<IActionResult> ClearAllFavorites([FromQuery] string? project_id = null)
        {
            Logger.LogInformation("📋 清空所有收藏請求，專案ID: {projectId}", project_id);

            try
            {
                // 驗證專案 ID
                var projectValidationResult = ValidateProjectId(project_id, allowNull: false);
                if (projectValidationResult != null)
                {
                    return projectValidationResult;
                }

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var deletedRows = await connection.ExecuteAsync(
                    "DELETE FROM user_favorites WHERE project_id = @projectId",
                    new { projectId = project_id });

                Logger.LogInformation("✅ 清空所有收藏成功: 刪除數量={deletedRows}, 專案ID: {projectId}", deletedRows, project_id);

                return Ok(new FavoriteResult
                {
                    Success = true,
                    Message = $"成功清空該專案所有收藏，共刪除 {deletedRows} 筆記錄"
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "❌ 清空所有收藏失敗: {error}, 專案ID: {projectId}", ex.Message, project_id);
                return StatusCode(500, new FavoriteResult
                {
                    Success = false,
                    Message = "清空收藏時發生錯誤，請稍後再試"
                });
            }
        }

        /// <summary>
        /// 獲取收藏統計
        /// </summary>
        /// <param name="project_id">專案ID</param>
        /// <returns>收藏統計資料</returns>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetFavoriteStatistics([FromQuery] string? project_id = null)
        {
            Logger.LogInformation("📋 獲取收藏統計請求，專案ID: {projectId}", project_id);

            try
            {
                // 驗證專案 ID
                var projectValidationResult = ValidateProjectId(project_id, allowNull: false);
                if (projectValidationResult != null)
                {
                    return projectValidationResult;
                }

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var totalFavorites = await connection.QuerySingleAsync<int>(
                    "SELECT COUNT(*) FROM user_favorites WHERE project_id = @projectId",
                    new { projectId = project_id });

                var favoritesWithViews = await connection.QuerySingleAsync<int>(
                    "SELECT COUNT(*) FROM user_favorites WHERE project_id = @projectId AND last_viewed_time IS NOT NULL",
                    new { projectId = project_id });

                var todayFavorites = await connection.QuerySingleAsync<int>(@"
                    SELECT COUNT(*) FROM user_favorites 
                    WHERE project_id = @projectId AND DATE(favorited_at) = CURRENT_DATE",
                    new { projectId = project_id });

                var recentFavorites = await connection.QueryAsync<dynamic>(@"
                    SELECT person_name, favorited_at 
                    FROM user_favorites 
                    WHERE project_id = @projectId
                    ORDER BY favorited_at DESC 
                    LIMIT 5",
                    new { projectId = project_id });

                var stats = new
                {
                    totalFavorites,
                    favoritesWithViews,
                    todayFavorites,
                    viewRate = totalFavorites > 0 ? Math.Round((double)favoritesWithViews / totalFavorites * 100, 2) : 0,
                    recentFavorites = recentFavorites.ToList()
                };

                Logger.LogInformation("✅ 收藏統計獲取成功: 總收藏={totalFavorites}, 今日收藏={todayFavorites}, 專案ID: {projectId}", 
                    totalFavorites, todayFavorites, project_id);

                return Ok(ApiResponse<object>.SuccessResult(stats, "獲取收藏統計成功"));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "❌ 獲取收藏統計失敗: {error}, 專案ID: {projectId}", ex.Message, project_id);
                return StatusCode(500, ApiResponse<object>.ErrorResult("獲取收藏統計時發生錯誤"));
            }
        }

        /// <summary>
        /// 獲取顯示時間（最後查看時間或收藏時間）
        /// </summary>
        private string GetDisplayTime(DateTime? lastViewedTime, DateTime favoritedAt)
        {
            var displayDateTime = lastViewedTime ?? favoritedAt;
            var now = DateTime.UtcNow;
            var diff = now - displayDateTime;

            if (diff.TotalMinutes < 1)
                return "剛剛";
            if (diff.TotalHours < 1)
                return $"{(int)diff.TotalMinutes} 分鐘前";
            if (diff.TotalDays < 1)
                return $"{(int)diff.TotalHours} 小時前";
            if (diff.TotalDays < 7)
                return $"{(int)diff.TotalDays} 天前";
            
            return displayDateTime.ToString("yyyy-MM-dd");
        }
    }
} 