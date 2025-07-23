// 專案管理控制器：用於管理家族樹專案的 CRUD 操作
// 主要功能：專案列表、新增、編輯、刪除(軟刪除)、搜尋、統計

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Npgsql;
using Dapper;
using familytree_backend.Constants;
using familytree_backend.Models;
using familytree_backend.Services;

namespace familytree_backend.Controllers
{
    [Route("api/[controller]")]
    public class ProjectController : BaseController
    {
        private readonly string _connectionString;

        public ProjectController(
            ILogger<ProjectController> logger,
            IConfigurationService configurationService) 
            : base(logger, configurationService)
        {
            _connectionString = configurationService.GetConnectionString();
        }

        // 移除硬編碼的 LogToFile 方法，使用繼承自 BaseController 的 Logger

        /// <summary>
        /// 獲取所有專案列表（排除已刪除的專案）
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetProjects([FromQuery] string? status = null, [FromQuery] string? search = null)
        {
            try
            {
                Logger.LogInformation($"開始獲取專案列表 - 狀態篩選: {status}, 搜尋關鍵字: {search}");
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var whereConditions = new List<string> { "status != 'deleted'" }; // 排除已刪除
                var parameters = new DynamicParameters();

                // 狀態篩選
                if (!string.IsNullOrEmpty(status) && status != "all")
                {
                    whereConditions.Add("status = @status");
                    parameters.Add("status", status);
                }

                // 搜尋功能
                if (!string.IsNullOrEmpty(search))
                {
                    whereConditions.Add("(project_name ILIKE @search OR project_description ILIKE @search)");
                    parameters.Add("search", $"%{search}%");
                }

                var whereClause = string.Join(" AND ", whereConditions);
                
                var sql = $@"
                    SELECT 
                        id,
                        user_id as UserId,
                        project_name as ProjectName,
                        project_description as ProjectDescription,
                        status,
                        created_at as CreatedAt,
                        completed_at as CompletedAt,
                        updated_at as UpdatedAt,
                        -- 統計相關專案的資料
                        (SELECT COUNT(*) FROM person_profile WHERE project_id = p.id) as MemberCount,
                        (SELECT COUNT(*) FROM relationship_layers WHERE project_id = p.id) as RelationshipCount
                    FROM projects p
                    WHERE {whereClause}
                    ORDER BY created_at DESC";
                
                var projects = await connection.QueryAsync<ProjectModel>(sql, parameters);
                
                Logger.LogInformation($"成功獲取 {projects.Count()} 個專案");
                return Ok(new ProjectListResponse 
                { 
                    Success = true,
                    Message = "成功獲取專案列表",
                    Projects = projects.ToList(),
                    TotalCount = projects.Count()
                });
            }
            catch (Exception ex)
            {
                Logger.LogInformation($"獲取專案列表時發生錯誤: {ex.Message}");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// 根據ID獲取單一專案
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProject(string id)
        {
            try
            {
                Logger.LogInformation($"開始獲取專案 ID: {id}");
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var sql = @"
                    SELECT 
                        id,
                        user_id as UserId,
                        project_name as ProjectName,
                        project_description as ProjectDescription,
                        status,
                        created_at as CreatedAt,
                        completed_at as CompletedAt,
                        updated_at as UpdatedAt,
                        (SELECT COUNT(*) FROM person_profile WHERE project_id = p.id) as MemberCount,
                        (SELECT COUNT(*) FROM relationship_layers WHERE project_id = p.id) as RelationshipCount
                    FROM projects p
                    WHERE id = @id AND status != 'deleted'";
                
                var project = await connection.QueryFirstOrDefaultAsync<ProjectModel>(sql, new { id });
                
                if (project == null)
                {
                    Logger.LogInformation($"專案 ID {id} 不存在或已被刪除");
                    return NotFound(new { success = false, message = "專案不存在" });
                }
                
                Logger.LogInformation($"成功獲取專案: {project.ProjectName}");
                return Ok(new { success = true, project });
            }
            catch (Exception ex)
            {
                Logger.LogInformation($"獲取專案 ID {id} 時發生錯誤: {ex.Message}");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// 新增專案
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequest request)
        {
            try
            {
                Logger.LogInformation($"開始建立新專案: {request.ProjectName}");
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                // 生成專案ID (userID-YYYYMMDDHHMMSS)
                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                var projectId = $"{request.UserId}-{timestamp}";
                
                var sql = @"
                    INSERT INTO projects (
                        id, 
                        user_id, 
                        project_name, 
                        project_description, 
                        status, 
                        created_at, 
                        updated_at
                    ) VALUES (
                        @id, 
                        @userId, 
                        @projectName, 
                        @projectDescription, 
                        @status, 
                        @createdAt, 
                        @updatedAt
                    )";
                
                var parameters = new
                {
                    id = projectId,
                    userId = request.UserId,
                    projectName = request.ProjectName,
                    projectDescription = request.ProjectDescription ?? "",
                    status = request.Status ?? "active",
                    createdAt = DateTime.UtcNow,
                    updatedAt = DateTime.UtcNow
                };
                
                await connection.ExecuteAsync(sql, parameters);
                
                Logger.LogInformation($"成功建立專案: {projectId} - {request.ProjectName}");
                
                // 返回新建立的專案資料
                return CreatedAtAction(nameof(GetProject), new { id = projectId }, new 
                { 
                    success = true, 
                    message = "專案建立成功",
                    projectId = projectId 
                });
            }
            catch (Exception ex)
            {
                Logger.LogInformation($"建立專案時發生錯誤: {ex.Message}");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// 更新專案
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProject(string id, [FromBody] UpdateProjectRequest request)
        {
            try
            {
                Logger.LogInformation($"開始更新專案 ID: {id}");
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                // 檢查專案是否存在且未被刪除
                var existsQuery = "SELECT COUNT(*) FROM projects WHERE id = @id AND status != 'deleted'";
                var exists = await connection.ExecuteScalarAsync<int>(existsQuery, new { id });
                
                if (exists == 0)
                {
                    Logger.LogInformation($"專案 ID {id} 不存在或已被刪除");
                    return NotFound(new { success = false, message = "專案不存在" });
                }
                
                var sql = @"
                    UPDATE projects 
                    SET 
                        project_name = @projectName,
                        project_description = @projectDescription,
                        status = @status,
                        updated_at = @updatedAt,
                        completed_at = CASE 
                            WHEN @status = 'completed' AND completed_at IS NULL THEN @updatedAt
                            WHEN @status != 'completed' THEN NULL
                            ELSE completed_at
                        END
                    WHERE id = @id AND status != 'deleted'";
                
                var parameters = new
                {
                    id,
                    projectName = request.ProjectName,
                    projectDescription = request.ProjectDescription ?? "",
                    status = request.Status ?? "active",
                    updatedAt = DateTime.UtcNow
                };
                
                var rowsAffected = await connection.ExecuteAsync(sql, parameters);
                
                if (rowsAffected == 0)
                {
                    Logger.LogInformation($"專案 ID {id} 更新失敗");
                    return NotFound(new { success = false, message = "專案更新失敗" });
                }
                
                Logger.LogInformation($"成功更新專案: {id} - {request.ProjectName}");
                return Ok(new { success = true, message = "專案更新成功" });
            }
            catch (Exception ex)
            {
                Logger.LogInformation($"更新專案 ID {id} 時發生錯誤: {ex.Message}");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// 軟刪除專案
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProject(string id)
        {
            try
            {
                Logger.LogInformation($"開始軟刪除專案 ID: {id}");
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                // 檢查專案是否存在且未被刪除
                var existsQuery = "SELECT COUNT(*) FROM projects WHERE id = @id AND status != 'deleted'";
                var exists = await connection.ExecuteScalarAsync<int>(existsQuery, new { id });
                
                if (exists == 0)
                {
                    Logger.LogInformation($"專案 ID {id} 不存在或已被刪除");
                    return NotFound(new { success = false, message = "專案不存在" });
                }
                
                // 軟刪除：只更新狀態為 'deleted'
                var sql = @"
                    UPDATE projects 
                    SET 
                        status = 'deleted',
                        updated_at = @updatedAt
                    WHERE id = @id AND status != 'deleted'";
                
                var rowsAffected = await connection.ExecuteAsync(sql, new { id, updatedAt = DateTime.UtcNow });
                
                if (rowsAffected == 0)
                {
                    Logger.LogInformation($"專案 ID {id} 軟刪除失敗");
                    return BadRequest(new { success = false, message = "專案刪除失敗" });
                }
                
                Logger.LogInformation($"成功軟刪除專案: {id}");
                return Ok(new { success = true, message = "專案已刪除" });
            }
            catch (Exception ex)
            {
                Logger.LogInformation($"軟刪除專案 ID {id} 時發生錯誤: {ex.Message}");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// 測試資料庫連接和編碼
        /// </summary>
        [HttpGet("test")]
        public async Task<IActionResult> TestProjectData()
        {
            try
            {
                Logger.LogInformation("開始測試專案資料");
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var sql = "SELECT id, user_id, project_name, status FROM projects LIMIT 1";
                var result = await connection.QueryAsync(sql);
                
                Logger.LogInformation("測試查詢完成");
                return Ok(new { success = true, rawData = result });
            }
            catch (Exception ex)
            {
                Logger.LogInformation($"測試時發生錯誤: {ex.Message}");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// 獲取專案統計資訊
        /// </summary>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetProjectStatistics()
        {
            try
            {
                Logger.LogInformation("開始獲取專案統計資訊");
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var sql = @"
                    SELECT 
                        COUNT(*) as TotalProjects,
                        COUNT(CASE WHEN status = 'active' THEN 1 END) as ActiveProjects,
                        COUNT(CASE WHEN status = 'completed' THEN 1 END) as CompletedProjects,
                        COUNT(CASE WHEN status = 'archived' THEN 1 END) as ArchivedProjects,
                        (SELECT COUNT(*) FROM person_profile WHERE project_id IN (SELECT id FROM projects WHERE status != 'deleted')) as TotalMembers,
                        (SELECT COUNT(*) FROM relationship_layers WHERE project_id IN (SELECT id FROM projects WHERE status != 'deleted')) as TotalRelationships
                    FROM projects 
                    WHERE status != 'deleted'";
                
                var statistics = await connection.QueryFirstOrDefaultAsync<ProjectStatistics>(sql);
                
                Logger.LogInformation("成功獲取專案統計資訊");
                return Ok(new { success = true, statistics });
            }
            catch (Exception ex)
            {
                Logger.LogInformation($"獲取專案統計資訊時發生錯誤: {ex.Message}");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }
    }
} 