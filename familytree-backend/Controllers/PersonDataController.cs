// 人員資料控制器 - 提供人員資料的CRUD操作和分頁查詢
// 優化重點：使用統一的資料存取服務，移除重複代碼，改善架構設計
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using familytree_backend.Constants;
using familytree_backend.Models;
using familytree_backend.Services;
using FamilyTree.Attributes;

namespace familytree_backend.Controllers
{
    /// <summary>
    /// 人員資料控制器
    /// 職責：提供人員資料的進階查詢、分頁、搜尋等功能
    /// 設計改善：使用統一的資料存取服務，移除重複代碼，改善架構設計
    /// </summary>
    [Route("api/[controller]")]
    [Authorize]
    public class PersonDataController : BaseController
    {
        private readonly IDataAccessServiceV2 _dataAccessService;
        private readonly PaginationConfiguration _paginationConfig;

        /// <summary>
        /// 人員資料控制器建構子
        /// 設計改善：使用統一的資料存取服務，避免直接操作資料庫
        /// </summary>
        public PersonDataController(
            ILogger<PersonDataController> logger,
            IConfigurationService configurationService,
            IDataAccessServiceV2 dataAccessService,
            IValidationService validationService,
            IAccessControlService accessControlService,
            ILoggingService loggingService) 
            : base(logger, configurationService, validationService, accessControlService, loggingService)
        {
            _dataAccessService = dataAccessService;
            _paginationConfig = configurationService.GetPaginationConfiguration();
        }

        /// <summary>
        /// 獲取人員資料列表 API（分頁查詢）
        /// 設計改善：使用統一的資料存取服務，移除重複的 SQL 查詢邏輯
        /// </summary>
        /// <param name="page">頁碼</param>
        /// <param name="pageSize">頁面大小</param>
        /// <param name="keyword">搜尋關鍵字</param>
        /// <param name="sortBy">排序欄位</param>
        /// <param name="sortOrder">排序方向（asc/desc）</param>
        /// <returns>分頁人員資料列表</returns>
        [HttpGet]
        [RequirePermission("person:read")]
        public async Task<IActionResult> GetPersonDataList(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 0, 
            [FromQuery] string? keyword = null,
            [FromQuery] string? sortBy = "id",
            [FromQuery] string? sortOrder = "desc")
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                var (userId, userRole) = GetUserInfo();
                
                LogRequestStart("獲取人員資料列表", new { 
                    UserId = userId, Page = page, PageSize = pageSize, 
                    Keyword = keyword, SortBy = sortBy, SortOrder = sortOrder 
                });
                
                // 步驟 1：正規化分頁參數（使用配置而非硬編碼）
                if (pageSize <= 0) pageSize = _paginationConfig.DefaultPageSize;
                var (normalizedPage, normalizedPageSize) = ValidateAndNormalizePagination(page, pageSize);

                // 步驟 2：使用統一的資料存取服務查詢資料（基於 user_id 隔離）
                var (personDataList, totalCount) = await _dataAccessService.GetPersonDataListAsync(
                    userId, userRole, normalizedPage, normalizedPageSize, keyword, sortBy, sortOrder);

                Logger.LogInformation("人員資料查詢統計：使用者 {UserId}，總筆數 {TotalCount}，查詢關鍵字 '{Keyword}'", 
                    userId, totalCount, keyword ?? "無");

                // 步驟 4：建立回應資料
                var response = new PersonDataListResponse
                {
                    Success = true,
                    Message = ApplicationConstants.ApiResponse.SuccessMessages.DataRetrievedSuccessfully,
                    PersonDataList = personDataList.ToList(),
                    Pagination = new PaginationInfo
                    {
                        CurrentPage = normalizedPage,
                        PageSize = normalizedPageSize,
                        TotalCount = totalCount,
                        TotalPages = (int)Math.Ceiling((double)totalCount / normalizedPageSize)
                    }
                };

                LogRequestComplete("獲取人員資料列表", response.PersonDataList.Count);
                return Ok(response);
            }, "獲取人員資料列表");
        }

        /// <summary>
        /// 測試 API 端點
        /// </summary>
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { message = "PersonDataController 測試成功", timestamp = DateTime.UtcNow });
        }


        /// <summary>
        /// 搜尋人員資料 API
        /// 設計改善：使用統一的資料存取服務，簡化搜尋邏輯
        /// </summary>
        /// <param name="query">搜尋查詢</param>
        /// <param name="page">頁碼</param>
        /// <param name="pageSize">頁面大小</param>
        /// <returns>搜尋結果</returns>
        [HttpGet("search")]
        [RequirePermission("person:read")]
        public async Task<IActionResult> SearchPersonData(
            [FromQuery] string query,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 0)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                var (userId, userRole) = GetUserInfo();
                
                LogRequestStart("搜尋人員資料", new { 
                    Query = query, UserId = userId, Page = page, PageSize = pageSize 
                });

                // 驗證參數
                if (string.IsNullOrWhiteSpace(query))
                {
                    return CreateErrorResponse("搜尋查詢不能為空");
                }

                // 正規化分頁參數
                if (pageSize <= 0) pageSize = _paginationConfig.DefaultPageSize;
                var (normalizedPage, normalizedPageSize) = ValidateAndNormalizePagination(page, pageSize);

                // 使用統一的資料存取服務進行搜尋（基於 user_id 隔離）
                var searchRequest = new SearchRequest
                {
                    Keyword = query,
                    Page = normalizedPage,
                    PageSize = normalizedPageSize,
                    SearchType = ApplicationConstants.Search.Types.Fuzzy
                };

                var searchResultsRaw = await _dataAccessService.SearchPersonsAsync(userId, userRole, searchRequest);
                var totalCount = searchResultsRaw.Count();

                // 轉換為 PersonSearchResult 格式
                var searchResults = searchResultsRaw.Select(p => new PersonSearchResult
                {
                    Id = p.Id,
                    Name = p.Name,
                    Gender = p.Gender,
                    Birthday = p.Birthday,
                    Nationality = p.Nationality,
                    Mobile = p.Mobile,
                    Phone = p.Phone,
                    Email = p.Email,
                    Address = p.CurrentAddress,
                    ProjectId = p.ProjectId,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                }).ToList();

                Logger.LogInformation("搜尋完成：查詢 '{Query}'，找到 {ResultCount} 筆結果", query, totalCount);

                var response = new PersonSearchResponse
                {
                    Success = true,
                    Message = $"搜尋完成，找到 {totalCount} 筆結果",
                    SearchResults = searchResults,
                    Pagination = new PaginationInfo
                    {
                        CurrentPage = normalizedPage,
                        PageSize = normalizedPageSize,
                        TotalCount = totalCount,
                        TotalPages = (int)Math.Ceiling((double)totalCount / normalizedPageSize)
                    }
                };

                LogRequestComplete("搜尋人員資料", searchResults.Count());
                return Ok(response);
            }, "搜尋人員資料");
        }

        /// <summary>
        /// 獲取人員資料統計 API
        /// 設計改善：使用統一的資料存取服務，簡化統計邏輯
        /// </summary>
        /// <returns>統計資料</returns>
        [HttpGet("statistics")]
        [RequirePermission("person:read")]
        public async Task<IActionResult> GetPersonDataStatistics()
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                var (userId, userRole) = GetUserInfo();
                
                LogRequestStart("獲取人員資料統計", new { UserId = userId });

                // 使用統一的資料存取服務獲取統計資料（基於 user_id 隔離）
                var (personDataList, totalCount) = await _dataAccessService.GetPersonDataListAsync(
                    userId, userRole, 1, int.MaxValue);

                var statistics = new PersonDataStatistics
                {
                    TotalCount = totalCount,
                    MaleCount = personDataList.Count(p => p.Gender?.ToLower() == "男"),
                    FemaleCount = personDataList.Count(p => p.Gender?.ToLower() == "女"),
                    UnknownGenderCount = personDataList.Count(p => string.IsNullOrEmpty(p.Gender) || p.Gender?.ToLower() != "男" && p.Gender?.ToLower() != "女"),
                    HasPhotoCount = personDataList.Count(p => !string.IsNullOrEmpty(p.Photo)),
                    HasContactInfoCount = personDataList.Count(p => !string.IsNullOrEmpty(p.Mobile) || !string.IsNullOrEmpty(p.Phone) || !string.IsNullOrEmpty(p.Email))
                };

                Logger.LogInformation("統計完成：使用者 {UserId}，總人數 {TotalCount}", userId, totalCount);

                var response = new PersonStatisticsResponse
                {
                    Success = true,
                    Message = "統計資料獲取成功",
                    Statistics = statistics
                };

                LogRequestComplete("獲取人員資料統計");
                return Ok(response);
            }, "獲取人員資料統計");
        }

        /// <summary>
        /// 根據 ID 獲取人員資料 API
        /// 設計改善：使用統一的資料存取服務，簡化查詢邏輯
        /// </summary>
        /// <param name="id">人員 ID</param>
        /// <returns>人員詳細資料</returns>
        [HttpGet("{id}")]
        [RequirePermission("person:read")]
        public async Task<IActionResult> GetPerson(int id)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                var (userId, userRole) = GetUserInfo();
                
                LogRequestStart("獲取人員資料", new { Id = id, UserId = userId });

                // 使用統一的資料存取服務查詢人員資料（基於 user_id 隔離）
                var personData = await _dataAccessService.GetPersonDataByIdAsync(id, userId, userRole);

                if (personData == null)
                {
                    return CreateNotFoundResponse("人員資料", id);
                }

                Logger.LogInformation("找到人員資料：ID {Id}，姓名 {Name}", id, personData.Name);

                var response = new PersonDataResponse
                {
                    Success = true,
                    Message = ApplicationConstants.ApiResponse.SuccessMessages.DataRetrievedSuccessfully,
                    PersonData = personData
                };

                LogRequestComplete("獲取人員資料");
                return Ok(response);
            }, "獲取人員資料");
        }

        /// <summary>
        /// 建立人員資料 API
        /// 設計改善：使用統一的資料存取服務，簡化建立邏輯
        /// </summary>
        /// <param name="person">人員資料</param>
        /// <returns>建立結果</returns>
        [HttpPost]
        [RequirePermission("person:create")]
        public async Task<IActionResult> CreatePerson([FromBody] PersonDataModel person)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                var (userId, userRole) = GetUserInfo();
                
                LogRequestStart("建立人員資料", new { Name = person.Name, UserId = userId });

                // 驗證人員資料
                var validationResult = ValidatePersonData(person);
                if (validationResult != null)
                {
                    return validationResult;
                }

                // 設定建立時間和使用者 ID
                person.UserId = userId;
                person.CreatedAt = DateTime.UtcNow;
                person.UpdatedAt = DateTime.UtcNow;

                // 使用統一的資料存取服務建立人員資料（基於 user_id 隔離）
                var newId = await _dataAccessService.AddPersonAsync(person, userId);

                Logger.LogInformation("人員資料建立成功：ID {Id}，姓名 {Name}", newId, person.Name);

                var response = new PersonDataResponse
                {
                    Success = true,
                    Message = ApplicationConstants.ApiResponse.SuccessMessages.DataCreatedSuccessfully,
                    PersonData = person
                };

                LogRequestComplete("建立人員資料");
                return CreatedAtAction(nameof(GetPerson), new { id = newId }, response);
            }, "建立人員資料");
        }

        /// <summary>
        /// 更新人員資料 API
        /// 設計改善：使用統一的資料存取服務，簡化更新邏輯
        /// </summary>
        /// <param name="id">人員 ID</param>
        /// <param name="person">更新的人員資料</param>
        /// <returns>更新結果</returns>
        [HttpPut("{id}")]
        [RequirePermission("person:update")]
        public async Task<IActionResult> UpdatePerson(int id, [FromBody] PersonDataModel person)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                var (userId, userRole) = GetUserInfo();
                
                LogRequestStart("更新人員資料", new { Id = id, Name = person.Name, UserId = userId });

                // 驗證人員資料
                var validationResult = ValidatePersonData(person);
                if (validationResult != null)
                {
                    return validationResult;
                }

                // 檢查人員是否存在
                var existingPerson = await _dataAccessService.GetPersonDataByIdAsync(id, userId, userRole);
                if (existingPerson == null)
                {
                    return CreateNotFoundResponse("人員資料", id);
                }

                // 設定更新時間和使用者 ID
                person.Id = id;
                person.UserId = userId;
                person.UpdatedAt = DateTime.UtcNow;

                // 使用統一的資料存取服務更新人員資料（基於 user_id 隔離）
                var success = await _dataAccessService.UpdatePersonAsync(id, person, userId, userRole);

                if (!success)
                {
                    return CreateErrorResponse("更新人員資料失敗");
                }

                Logger.LogInformation("人員資料更新成功：ID {Id}，姓名 {Name}", id, person.Name);

                var response = new PersonDataResponse
                {
                    Success = true,
                    Message = ApplicationConstants.ApiResponse.SuccessMessages.DataUpdatedSuccessfully,
                    PersonData = person
                };

                LogRequestComplete("更新人員資料");
                return Ok(response);
            }, "更新人員資料");
        }

        /// <summary>
        /// 刪除人員資料 API
        /// 設計改善：使用統一的資料存取服務，簡化刪除邏輯
        /// </summary>
        /// <param name="id">人員 ID</param>
        /// <returns>刪除結果</returns>
        [HttpDelete("{id}")]
        [RequirePermission("person:delete")]
        public async Task<IActionResult> DeletePerson(int id)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                var (userId, userRole) = GetUserInfo();
                
                LogRequestStart("刪除人員資料", new { Id = id, UserId = userId });

                // 檢查人員是否存在
                var existingPerson = await _dataAccessService.GetPersonDataByIdAsync(id, userId, userRole);
                if (existingPerson == null)
                {
                    return CreateNotFoundResponse("人員資料", id);
                }

                // 使用統一的資料存取服務刪除人員資料（基於 user_id 隔離）
                var success = await _dataAccessService.DeletePersonAsync(id, userId, userRole);

                if (!success)
                {
                    return CreateErrorResponse("刪除人員資料失敗");
                }

                Logger.LogInformation("人員資料刪除成功：ID {Id}，姓名 {Name}", id, existingPerson.Name);

                var response = new ApiResponse
                {
                    Success = true,
                    Message = ApplicationConstants.ApiResponse.SuccessMessages.DataDeletedSuccessfully
                };

                LogRequestComplete("刪除人員資料");
                return Ok(response);
            }, "刪除人員資料");
        }

        #region 私有輔助方法

        /// <summary>
        /// 驗證人員資料
        /// 設計理念：統一的人員資料驗證邏輯
        /// </summary>
        private IActionResult? ValidatePersonData(PersonDataModel person)
        {
            if (person == null)
            {
                return CreateErrorResponse("人員資料不能為空");
            }

            if (string.IsNullOrWhiteSpace(person.Name))
            {
                return CreateErrorResponse("姓名為必填項目");
            }

            if (person.Name.Length > ApplicationConstants.Database.NameMaxLength)
            {
                return CreateErrorResponse($"姓名長度不能超過 {ApplicationConstants.Database.NameMaxLength} 個字元");
            }

            if (!string.IsNullOrEmpty(person.Email) && person.Email.Length > ApplicationConstants.Database.EmailMaxLength)
            {
                return CreateErrorResponse($"電子信箱長度不能超過 {ApplicationConstants.Database.EmailMaxLength} 個字元");
            }

            if (!string.IsNullOrEmpty(person.Mobile) && person.Mobile.Length > ApplicationConstants.Database.PhoneMaxLength)
            {
                return CreateErrorResponse($"手機號碼長度不能超過 {ApplicationConstants.Database.PhoneMaxLength} 個字元");
            }

            return null;
        }

        #endregion
    }

    #region 回應模型

    /// <summary>
    /// 人員資料列表回應
    /// </summary>
    public class PersonDataListResponse : ApiResponse
    {
        public List<PersonDataModel> PersonDataList { get; set; } = new();
        public PaginationInfo Pagination { get; set; } = new();
    }

    /// <summary>
    /// 人員資料回應
    /// </summary>
    public class PersonDataResponse : ApiResponse
    {
        public PersonDataModel PersonData { get; set; } = new();
    }

    /// <summary>
    /// 人員搜尋回應
    /// </summary>
    public class PersonSearchResponse : ApiResponse
    {
        public List<PersonSearchResult> SearchResults { get; set; } = new();
        public PaginationInfo Pagination { get; set; } = new();
    }

    /// <summary>
    /// 人員統計回應
    /// </summary>
    public class PersonStatisticsResponse : ApiResponse
    {
        public PersonDataStatistics Statistics { get; set; } = new();
    }

    /// <summary>
    /// 人員資料統計
    /// </summary>
    public class PersonDataStatistics
    {
        public int TotalCount { get; set; }
        public int MaleCount { get; set; }
        public int FemaleCount { get; set; }
        public int UnknownGenderCount { get; set; }
        public int HasPhotoCount { get; set; }
        public int HasContactInfoCount { get; set; }
    }

    /// <summary>
    /// 分頁資訊
    /// </summary>
    public class PaginationInfo
    {
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }

    #endregion
} 