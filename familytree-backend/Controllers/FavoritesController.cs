// 收藏功能控制器：提供用戶收藏管理功能
// 主要功能：添加收藏、刪除收藏、獲取收藏列表、更新查看時間

using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Dapper;
using familytree_backend.Models;

namespace familytree_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FavoritesController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly ILogger<FavoritesController> _logger;

        public FavoritesController(IConfiguration configuration, ILogger<FavoritesController> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("找不到資料庫連接字符串");
            _logger = logger;
        }

        /// <summary>
        /// 獲取用戶收藏列表
        /// </summary>
        /// <returns>收藏列表</returns>
        [HttpGet]
        public async Task<IActionResult> GetFavorites()
        {
            _logger.LogInformation("📋 獲取用戶收藏列表請求");

            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT f.id, f.person_id, f.person_name, f.last_viewed_time, f.favorited_at,
                           f.created_at, f.updated_at
                    FROM user_favorites f
                    ORDER BY 
                        CASE WHEN f.last_viewed_time IS NOT NULL THEN f.last_viewed_time ELSE f.favorited_at END DESC";

                var favorites = await connection.QueryAsync<UserFavorite>(sql);
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

                _logger.LogInformation("✅ 成功獲取收藏列表: 數量={count}", favoriteItems.Count);

                return Ok(new FavoriteListResult
                {
                    Success = true,
                    Message = $"獲取收藏列表成功，共 {favoriteItems.Count} 筆",
                    Data = favoriteItems
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 獲取收藏列表失敗: {error}", ex.Message);
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
        /// <returns>操作結果</returns>
        [HttpPost]
        public async Task<IActionResult> AddFavorite([FromBody] FavoriteRequest request)
        {
            _logger.LogInformation("📋 添加收藏請求: PersonId={personId}, PersonName='{personName}'", 
                request.PersonId, request.PersonName);

            try
            {
                // 參數驗證
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    var errorMessage = string.Join("; ", errors);
                    _logger.LogWarning("⚠️  請求參數驗證失敗: {errors}", errorMessage);
                    return BadRequest(new FavoriteResult
                    {
                        Success = false,
                        Message = $"參數驗證失敗: {errorMessage}"
                    });
                }

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // 檢查是否已經收藏
                var existingFavorite = await connection.QueryFirstOrDefaultAsync<int?>(
                    "SELECT id FROM user_favorites WHERE person_id = @personId",
                    new { personId = request.PersonId });

                if (existingFavorite.HasValue)
                {
                    _logger.LogInformation("⚠️  人員已經收藏: PersonId={personId}", request.PersonId);
                    return Conflict(new FavoriteResult
                    {
                        Success = false,
                        Message = $"該人員 '{request.PersonName}' 已經收藏過了"
                    });
                }

                // 驗證人員是否存在
                var personExists = await connection.QueryFirstOrDefaultAsync<bool>(
                    "SELECT COUNT(*) > 0 FROM person_profile WHERE id = @personId",
                    new { personId = request.PersonId });

                if (!personExists)
                {
                    _logger.LogWarning("⚠️  人員不存在: PersonId={personId}", request.PersonId);
                    return NotFound(new FavoriteResult
                    {
                        Success = false,
                        Message = $"找不到 ID 為 {request.PersonId} 的人員"
                    });
                }

                // 添加收藏
                var now = DateTime.UtcNow;
                var sql = @"
                    INSERT INTO user_favorites (person_id, person_name, favorited_at, created_at, updated_at)
                    VALUES (@personId, @personName, @now, @now, @now)
                    RETURNING id";

                var favoriteId = await connection.QuerySingleAsync<int>(sql, new
                {
                    personId = request.PersonId,
                    personName = request.PersonName,
                    now
                });

                _logger.LogInformation("✅ 收藏添加成功: FavoriteId={favoriteId}, PersonId={personId}, PersonName='{personName}'", 
                    favoriteId, request.PersonId, request.PersonName);

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
                _logger.LogError(ex, "❌ 添加收藏失敗: PersonId={personId}, PersonName='{personName}', 錯誤={error}", 
                    request.PersonId, request.PersonName, ex.Message);
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
        /// <returns>操作結果</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoveFavorite(int id)
        {
            _logger.LogInformation("📋 刪除收藏請求: FavoriteId={favoriteId}", id);

            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // 獲取收藏資訊
                var favorite = await connection.QueryFirstOrDefaultAsync<UserFavorite>(
                    "SELECT id, person_id, person_name, favorited_at FROM user_favorites WHERE id = @id",
                    new { id });

                if (favorite == null)
                {
                    _logger.LogWarning("⚠️  收藏不存在: FavoriteId={favoriteId}", id);
                    return NotFound(new FavoriteResult
                    {
                        Success = false,
                        Message = $"找不到 ID 為 {id} 的收藏"
                    });
                }

                // 刪除收藏
                var deletedRows = await connection.ExecuteAsync(
                    "DELETE FROM user_favorites WHERE id = @id",
                    new { id });

                if (deletedRows > 0)
                {
                    _logger.LogInformation("✅ 收藏刪除成功: FavoriteId={favoriteId}, PersonName='{personName}'", 
                        id, favorite.PersonName);

                    return Ok(new FavoriteResult
                    {
                        Success = true,
                        Message = $"成功取消收藏 '{favorite.PersonName}'"
                    });
                }
                else
                {
                    _logger.LogWarning("⚠️  收藏刪除失敗，沒有刪除任何記錄: FavoriteId={favoriteId}", id);
                    return BadRequest(new FavoriteResult
                    {
                        Success = false,
                        Message = "刪除收藏失敗"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 刪除收藏失敗: FavoriteId={favoriteId}, 錯誤={error}", id, ex.Message);
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
        /// <returns>操作結果</returns>
        [HttpDelete("person/{personId}")]
        public async Task<IActionResult> RemoveFavoriteByPersonId(int personId)
        {
            _logger.LogInformation("📋 通過人員ID刪除收藏請求: PersonId={personId}", personId);

            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // 獲取收藏資訊
                var favorite = await connection.QueryFirstOrDefaultAsync<UserFavorite>(
                    "SELECT id, person_id, person_name, favorited_at FROM user_favorites WHERE person_id = @personId",
                    new { personId });

                if (favorite == null)
                {
                    _logger.LogWarning("⚠️  該人員未被收藏: PersonId={personId}", personId);
                    return NotFound(new FavoriteResult
                    {
                        Success = false,
                        Message = $"該人員未被收藏"
                    });
                }

                // 刪除收藏
                var deletedRows = await connection.ExecuteAsync(
                    "DELETE FROM user_favorites WHERE person_id = @personId",
                    new { personId });

                if (deletedRows > 0)
                {
                    _logger.LogInformation("✅ 通過人員ID刪除收藏成功: PersonId={personId}, PersonName='{personName}'", 
                        personId, favorite.PersonName);

                    return Ok(new FavoriteResult
                    {
                        Success = true,
                        Message = $"成功取消收藏 '{favorite.PersonName}'"
                    });
                }
                else
                {
                    _logger.LogWarning("⚠️  通過人員ID刪除收藏失敗: PersonId={personId}", personId);
                    return BadRequest(new FavoriteResult
                    {
                        Success = false,
                        Message = "取消收藏失敗"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 通過人員ID刪除收藏失敗: PersonId={personId}, 錯誤={error}", personId, ex.Message);
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
        /// <returns>是否已收藏</returns>
        [HttpGet("check/{personId}")]
        public async Task<IActionResult> CheckFavoriteStatus(int personId)
        {
            _logger.LogInformation("📋 檢查收藏狀態請求: PersonId={personId}", personId);

            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var favoriteId = await connection.QueryFirstOrDefaultAsync<int?>(
                    "SELECT id FROM user_favorites WHERE person_id = @personId",
                    new { personId });

                var isFavorited = favoriteId.HasValue;

                _logger.LogInformation("✅ 收藏狀態檢查完成: PersonId={personId}, IsFavorited={isFavorited}", 
                    personId, isFavorited);

                return Ok(ApiResponse<object>.SuccessResult(new
                {
                    personId,
                    isFavorited,
                    favoriteId = favoriteId
                }, "檢查收藏狀態成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 檢查收藏狀態失敗: PersonId={personId}, 錯誤={error}", personId, ex.Message);
                return StatusCode(500, ApiResponse<object>.ErrorResult("檢查收藏狀態時發生錯誤"));
            }
        }

        /// <summary>
        /// 更新收藏的查看時間
        /// </summary>
        /// <param name="personId">人員ID</param>
        /// <returns>操作結果</returns>
        [HttpPut("view/{personId}")]
        public async Task<IActionResult> UpdateViewTime(int personId)
        {
            _logger.LogInformation("📋 更新收藏查看時間請求: PersonId={personId}", personId);

            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var now = DateTime.UtcNow;
                var updatedRows = await connection.ExecuteAsync(@"
                    UPDATE user_favorites 
                    SET last_viewed_time = @now, updated_at = @now
                    WHERE person_id = @personId",
                    new { personId, now });

                if (updatedRows > 0)
                {
                    _logger.LogInformation("✅ 收藏查看時間更新成功: PersonId={personId}", personId);

                    return Ok(ApiResponse<object>.SuccessResult(new
                    {
                        personId,
                        lastViewedTime = now
                    }, "查看時間更新成功"));
                }
                else
                {
                    _logger.LogWarning("⚠️  該人員未被收藏，無法更新查看時間: PersonId={personId}", personId);
                    return NotFound(ApiResponse<object>.ErrorResult("該人員未被收藏"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 更新收藏查看時間失敗: PersonId={personId}, 錯誤={error}", personId, ex.Message);
                return StatusCode(500, ApiResponse<object>.ErrorResult("更新查看時間時發生錯誤"));
            }
        }

        /// <summary>
        /// 清空所有收藏
        /// </summary>
        /// <returns>操作結果</returns>
        [HttpDelete("clear")]
        public async Task<IActionResult> ClearAllFavorites()
        {
            _logger.LogInformation("📋 清空所有收藏請求");

            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var deletedRows = await connection.ExecuteAsync("DELETE FROM user_favorites");

                _logger.LogInformation("✅ 清空所有收藏成功: 刪除數量={deletedRows}", deletedRows);

                return Ok(new FavoriteResult
                {
                    Success = true,
                    Message = $"成功清空所有收藏，共刪除 {deletedRows} 筆記錄"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 清空所有收藏失敗: {error}", ex.Message);
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
        /// <returns>收藏統計資料</returns>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetFavoriteStatistics()
        {
            _logger.LogInformation("📋 獲取收藏統計請求");

            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var totalFavorites = await connection.QuerySingleAsync<int>("SELECT COUNT(*) FROM user_favorites");
                var favoritesWithViews = await connection.QuerySingleAsync<int>("SELECT COUNT(*) FROM user_favorites WHERE last_viewed_time IS NOT NULL");
                var todayFavorites = await connection.QuerySingleAsync<int>(@"
                    SELECT COUNT(*) FROM user_favorites 
                    WHERE DATE(favorited_at) = CURRENT_DATE");

                var recentFavorites = await connection.QueryAsync<dynamic>(@"
                    SELECT person_name, favorited_at 
                    FROM user_favorites 
                    ORDER BY favorited_at DESC 
                    LIMIT 5");

                var stats = new
                {
                    totalFavorites,
                    favoritesWithViews,
                    todayFavorites,
                    viewRate = totalFavorites > 0 ? Math.Round((double)favoritesWithViews / totalFavorites * 100, 2) : 0,
                    recentFavorites = recentFavorites.ToList()
                };

                _logger.LogInformation("✅ 收藏統計獲取成功: 總收藏={totalFavorites}, 今日收藏={todayFavorites}", 
                    totalFavorites, todayFavorites);

                return Ok(ApiResponse<object>.SuccessResult(stats, "獲取收藏統計成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 獲取收藏統計失敗: {error}", ex.Message);
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