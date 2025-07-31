// 專案管理控制器：用於管理家族樹專案的 CRUD 操作
// 主要功能：專案列表、新增、編輯、刪除(軟刪除)、搜尋、統計
// 設計改善：使用統一的資料存取服務，移除重複代碼，改善架構設計

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using familytree_backend.Constants;
using familytree_backend.Models;
using familytree_backend.Services;

namespace familytree_backend.Controllers
{
    /// <summary>
    /// 專案管理控制器
    /// 職責：提供專案的 CRUD 操作、搜尋、統計等功能
    /// 設計改善：使用統一的資料存取服務，移除重複的 SQL 查詢邏輯
    /// </summary>
    [Route("api/[controller]")]
    public class ProjectController : BaseController
    {
        private readonly IDataAccessService _dataAccessService;

        /// <summary>
        /// 專案管理控制器建構子
        /// 設計改善：使用統一的資料存取服務，避免直接操作資料庫
        /// </summary>
        public ProjectController(
            ILogger<ProjectController> logger,
            IConfigurationService configurationService,
            IDataAccessService dataAccessService,
            IValidationService validationService,
            IAccessControlService accessControlService,
            ILoggingService loggingService) 
            : base(logger, configurationService, validationService, accessControlService, loggingService)
        {
            _dataAccessService = dataAccessService;
        }

        /// <summary>
        /// 獲取所有專案列表（排除已刪除的專案）
        /// 設計改善：使用統一的資料存取服務，簡化查詢邏輯
        /// </summary>
        /// <param name="status">狀態篩選</param>
        /// <param name="search">搜尋關鍵字</param>
        /// <returns>專案列表</returns>
        [HttpGet]
        public async Task<IActionResult> GetProjects([FromQuery] string? status = null, [FromQuery] string? search = null)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("獲取專案列表", new { Status = status, Search = search });

                // 使用統一的資料存取服務獲取專案列表
                var allProjects = await _dataAccessService.GetProjectListAsync("system"); // 暫時使用系統用戶
                
                // 在控制器中進行篩選
                var projects = allProjects.AsEnumerable();
                
                if (!string.IsNullOrEmpty(status))
                {
                    projects = projects.Where(p => p.Status?.Equals(status, StringComparison.OrdinalIgnoreCase) == true);
                }
                
                if (!string.IsNullOrEmpty(search))
                {
                    projects = projects.Where(p => 
                        p.ProjectName?.Contains(search, StringComparison.OrdinalIgnoreCase) == true ||
                        p.ProjectDescription?.Contains(search, StringComparison.OrdinalIgnoreCase) == true);
                }

                Logger.LogInformation("成功獲取 {Count} 個專案", projects.Count());

                var response = new ProjectListResponse
                {
                    Success = true,
                    Message = "成功獲取專案列表",
                    Projects = projects.ToList(),
                    TotalCount = projects.Count()
                };

                LogRequestComplete("獲取專案列表", projects.Count());
                return Ok(response);
            }, "獲取專案列表");
        }

        /// <summary>
        /// 根據ID獲取單一專案
        /// 設計改善：使用統一的資料存取服務，簡化查詢邏輯
        /// </summary>
        /// <param name="id">專案 ID</param>
        /// <returns>專案詳細資料</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProject(string id)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("獲取專案", new { Id = id });

                // 參數驗證
                if (string.IsNullOrWhiteSpace(id))
                {
                    return CreateErrorResponse("專案 ID 不能為空");
                }

                // 使用統一的資料存取服務獲取專案資料
                var project = await _dataAccessService.GetProjectByIdAsync(id);

                if (project == null)
                {
                    return CreateNotFoundResponse("專案", id);
                }

                Logger.LogInformation("成功獲取專案：ID {Id}，名稱 {Name}", id, project.ProjectName);

                var response = new ProjectResponse
                {
                    Success = true,
                    Message = "成功獲取專案資料",
                    Project = project
                };

                LogRequestComplete("獲取專案");
                return Ok(response);
            }, "獲取專案");
        }

        /// <summary>
        /// 建立新專案
        /// 設計改善：使用統一的資料存取服務，簡化建立邏輯
        /// </summary>
        /// <param name="request">建立專案請求</param>
        /// <returns>建立結果</returns>
        [HttpPost]
        public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequest request)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("建立專案", new { Name = request.ProjectName, Description = request.ProjectDescription });

                // 參數驗證
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    var errorMessage = string.Join("; ", errors);
                    Logger.LogWarning("請求參數驗證失敗: {errors}", errorMessage);
                    return CreateErrorResponse($"參數驗證失敗: {errorMessage}");
                }

                if (string.IsNullOrWhiteSpace(request.ProjectName))
                {
                    return CreateErrorResponse("專案名稱不能為空");
                }

                if (request.ProjectName.Length > ApplicationConstants.Database.ProjectNameMaxLength)
                {
                    return CreateErrorResponse($"專案名稱長度不能超過 {ApplicationConstants.Database.ProjectNameMaxLength} 個字元");
                }

                // 使用統一的資料存取服務建立專案
                var userId = request.UserId ?? "system";
                var newId = await _dataAccessService.CreateProjectAsync(request, userId);
                
                // 建立專案模型以供回應使用
                var project = new ProjectModel
                {
                    Id = newId ?? "",
                    UserId = userId,
                    ProjectName = request.ProjectName,
                    ProjectDescription = request.ProjectDescription,
                    Status = "active",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                Logger.LogInformation("成功建立專案：ID {Id}，名稱 {Name}", newId, project.ProjectName);

                var response = new
                {
                    success = true,
                    message = "專案建立成功",
                    projectId = newId
                };

                LogRequestComplete("建立專案");
                return CreatedAtAction(nameof(GetProject), new { id = newId }, response);
            }, "建立專案");
        }

        /// <summary>
        /// 更新專案
        /// 設計改善：使用統一的資料存取服務，簡化更新邏輯
        /// </summary>
        /// <param name="id">專案 ID</param>
        /// <param name="request">更新專案請求</param>
        /// <returns>更新結果</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProject(string id, [FromBody] UpdateProjectRequest request)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("更新專案", new { Id = id, Name = request.ProjectName });

                // 參數驗證
                if (string.IsNullOrWhiteSpace(id))
                {
                    return CreateErrorResponse("專案 ID 不能為空");
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    var errorMessage = string.Join("; ", errors);
                    Logger.LogWarning("請求參數驗證失敗: {errors}", errorMessage);
                    return CreateErrorResponse($"參數驗證失敗: {errorMessage}");
                }

                if (string.IsNullOrWhiteSpace(request.ProjectName))
                {
                    return CreateErrorResponse("專案名稱不能為空");
                }

                if (request.ProjectName.Length > ApplicationConstants.Database.ProjectNameMaxLength)
                {
                    return CreateErrorResponse($"專案名稱長度不能超過 {ApplicationConstants.Database.ProjectNameMaxLength} 個字元");
                }

                // 檢查專案是否存在
                var existingProject = await _dataAccessService.GetProjectByIdAsync(id);
                if (existingProject == null)
                {
                    return CreateNotFoundResponse("專案", id);
                }

                // 使用統一的資料存取服務更新專案
                var success = await _dataAccessService.UpdateProjectAsync(id, request);
                
                // 建立更新後的專案模型以供回應使用
                var project = new ProjectModel
                {
                    Id = id,
                    UserId = existingProject.UserId,
                    ProjectName = request.ProjectName,
                    ProjectDescription = request.ProjectDescription,
                    Status = request.Status ?? existingProject.Status,
                    CompletedAt = request.Status == "completed" ? DateTime.UtcNow : existingProject.CompletedAt,
                    UpdatedAt = DateTime.UtcNow
                };

                if (!success)
                {
                    return CreateErrorResponse("更新專案失敗");
                }

                Logger.LogInformation("成功更新專案：ID {Id}，名稱 {Name}", id, project.ProjectName);

                var response = new ProjectResponse
                {
                    Success = true,
                    Message = "專案更新成功",
                    Project = project
                };

                LogRequestComplete("更新專案");
                return Ok(response);
            }, "更新專案");
        }

        /// <summary>
        /// 刪除專案（軟刪除）
        /// 設計改善：使用統一的資料存取服務，簡化刪除邏輯
        /// </summary>
        /// <param name="id">專案 ID</param>
        /// <returns>刪除結果</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProject(string id)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("刪除專案", new { Id = id });

                // 參數驗證
                if (string.IsNullOrWhiteSpace(id))
                {
                    return CreateErrorResponse("專案 ID 不能為空");
                }

                // 檢查專案是否存在
                var existingProject = await _dataAccessService.GetProjectByIdAsync(id);
                if (existingProject == null)
                {
                    return CreateNotFoundResponse("專案", id);
                }

                // 使用統一的資料存取服務刪除專案
                var success = await _dataAccessService.DeleteProjectAsync(id, "system");

                if (!success)
                {
                    return CreateErrorResponse("刪除專案失敗");
                }

                Logger.LogInformation("成功刪除專案：ID {Id}，名稱 {Name}", id, existingProject.ProjectName);

                var response = new ApiResponse
                {
                    Success = true,
                    Message = "專案刪除成功"
                };

                LogRequestComplete("刪除專案");
                return Ok(response);
            }, "刪除專案");
        }

        /// <summary>
        /// 測試專案資料 API
        /// </summary>
        [HttpGet("test")]
        public async Task<IActionResult> TestProjectData()
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("測試專案資料");

                // 使用統一的資料存取服務獲取專案統計
                var projects = await _dataAccessService.GetProjectListAsync("system");

                var testData = new
                {
                    TotalProjects = projects.Count(),
                    ActiveProjects = projects.Count(p => p.Status == "active"),
                    CompletedProjects = projects.Count(p => p.Status == "completed"),
                    Message = "專案資料測試成功",
                    Timestamp = DateTime.UtcNow
                };

                LogRequestComplete("測試專案資料");
                return Ok(testData);
            }, "測試專案資料");
        }

        /// <summary>
        /// 獲取專案統計資料
        /// 設計改善：使用統一的資料存取服務，簡化統計邏輯
        /// </summary>
        /// <returns>專案統計資料</returns>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetProjectStatistics()
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("獲取專案統計");

                // 使用統一的資料存取服務獲取專案統計
                var projects = await _dataAccessService.GetProjectListAsync("system");

                var statistics = new ProjectStatistics
                {
                    TotalProjects = projects.Count(),
                    ActiveProjects = projects.Count(p => p.Status == "active"),
                    CompletedProjects = projects.Count(p => p.Status == "completed"),
                    DeletedProjects = projects.Count(p => p.Status == "deleted"),
                    TotalMembers = projects.Sum(p => p.MemberCount),
                    TotalRelationships = projects.Sum(p => p.RelationshipCount),
                    AverageMembersPerProject = projects.Any() ? (double)projects.Sum(p => p.MemberCount) / projects.Count() : 0,
                    AverageRelationshipsPerProject = projects.Any() ? (double)projects.Sum(p => p.RelationshipCount) / projects.Count() : 0
                };

                Logger.LogInformation("專案統計完成：總專案 {Total}，活躍 {Active}，完成 {Completed}", 
                    statistics.TotalProjects, statistics.ActiveProjects, statistics.CompletedProjects);

                var response = new ProjectStatisticsResponse
                {
                    Success = true,
                    Message = "專案統計資料獲取成功",
                    Statistics = statistics
                };

                LogRequestComplete("獲取專案統計");
                return Ok(response);
            }, "獲取專案統計");
        }
    }

    #region 回應模型

    /// <summary>
    /// 專案列表回應
    /// </summary>
    public class ProjectListResponse : ApiResponse
    {
        public List<ProjectModel> Projects { get; set; } = new();
        public int TotalCount { get; set; }
    }

    /// <summary>
    /// 專案回應
    /// </summary>
    public class ProjectResponse : ApiResponse
    {
        public ProjectModel Project { get; set; } = new();
    }

    /// <summary>
    /// 專案統計回應
    /// </summary>
    public class ProjectStatisticsResponse : ApiResponse
    {
        public ProjectStatistics Statistics { get; set; } = new();
    }

    /// <summary>
    /// 專案統計資料
    /// </summary>
    public class ProjectStatistics
    {
        public int TotalProjects { get; set; }
        public int ActiveProjects { get; set; }
        public int CompletedProjects { get; set; }
        public int DeletedProjects { get; set; }
        public int TotalMembers { get; set; }
        public int TotalRelationships { get; set; }
        public double AverageMembersPerProject { get; set; }
        public double AverageRelationshipsPerProject { get; set; }
    }

    #endregion
} 