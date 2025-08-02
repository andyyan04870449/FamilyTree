// 人員資料控制器 V2 - 基於使用者權限的資料存取
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using familytree_backend.Constants;
using familytree_backend.Models;
using familytree_backend.Services;

namespace familytree_backend.Controllers
{
    /// <summary>
    /// 人員資料控制器 V2
    /// 基於使用者權限的資料存取
    /// </summary>
    [Route("api/v2/persondata")]
    [Authorize]
    public class PersonDataV2Controller : BaseController
    {
        private readonly IDataAccessServiceV2 _dataAccessService;
        private readonly PaginationConfiguration _paginationConfig;

        /// <summary>
        /// 人員資料控制器建構子
        /// </summary>
        public PersonDataV2Controller(
            ILogger<PersonDataV2Controller> logger,
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
        /// </summary>
        [HttpGet]
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

                // 正規化分頁參數
                if (pageSize <= 0) pageSize = _paginationConfig.DefaultPageSize;
                var (normalizedPage, normalizedPageSize) = ValidateAndNormalizePagination(page, pageSize);

                // 使用 V2 資料存取服務查詢資料
                var (personDataList, totalCount) = await _dataAccessService.GetPersonDataListAsync(
                    userId, userRole, normalizedPage, normalizedPageSize, keyword, sortBy, sortOrder);

                Logger.LogInformation("人員資料查詢統計：使用者 {UserId}，總筆數 {TotalCount}，查詢關鍵字 '{Keyword}'", 
                    userId, totalCount, keyword ?? "無");

                // 建立回應資料
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
        /// 根據 ID 獲取人員資料 API
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPerson(int id)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                var (userId, userRole) = GetUserInfo();
                
                LogRequestStart("獲取人員資料", new { Id = id, UserId = userId });

                // 使用 V2 資料存取服務查詢人員資料
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
        /// </summary>
        [HttpPost]
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

                // 使用 V2 資料存取服務建立人員資料
                var newId = await _dataAccessService.AddPersonAsync(person, userId);

                Logger.LogInformation("人員資料建立成功：ID {Id}，姓名 {Name}", newId, person.Name);

                person.Id = newId;
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
        /// </summary>
        [HttpPut("{id}")]
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

                // 使用 V2 資料存取服務更新人員資料
                var success = await _dataAccessService.UpdatePersonAsync(id, person, userId, userRole);

                if (!success)
                {
                    return CreateErrorResponse("更新人員資料失敗");
                }

                Logger.LogInformation("人員資料更新成功：ID {Id}，姓名 {Name}", id, person.Name);

                person.Id = id;
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
        /// </summary>
        [HttpDelete("{id}")]
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

                // 使用 V2 資料存取服務刪除人員資料
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

        /// <summary>
        /// 搜尋人員資料 API
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> SearchPersonData(
            [FromQuery] string query,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 0,
            [FromQuery] string searchType = "fuzzy")
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                var (userId, userRole) = GetUserInfo();
                
                LogRequestStart("搜尋人員資料", new { 
                    Query = query, UserId = userId, Page = page, PageSize = pageSize, SearchType = searchType 
                });

                // 驗證參數
                if (string.IsNullOrWhiteSpace(query))
                {
                    return CreateErrorResponse("搜尋查詢不能為空");
                }

                // 正規化分頁參數
                if (pageSize <= 0) pageSize = _paginationConfig.DefaultPageSize;
                var (normalizedPage, normalizedPageSize) = ValidateAndNormalizePagination(page, pageSize);

                // 使用 V2 資料存取服務進行搜尋
                var searchRequest = new SearchRequest
                {
                    Keyword = query,
                    Page = normalizedPage,
                    PageSize = normalizedPageSize,
                    SearchType = searchType
                };

                var searchResults = await _dataAccessService.SearchPersonsAsync(userId, userRole, searchRequest);
                var resultList = searchResults.ToList();

                Logger.LogInformation("搜尋完成：查詢 '{Query}'，找到 {ResultCount} 筆結果", query, resultList.Count);

                // 轉換為搜尋結果格式
                var searchResultList = resultList.Select(p => new PersonSearchResult
                {
                    Id = p.Id,
                    Name = p.Name,
                    Gender = p.Gender,
                    Birthday = p.Birthday,
                    Nationality = p.Nationality,
                    Email = p.Email,
                    Mobile = p.Mobile,
                    CurrentEmployer = p.CurrentEmployer,
                    Address = p.Address
                }).ToList();

                var response = new PersonSearchResponse
                {
                    Success = true,
                    Message = $"搜尋完成，找到 {resultList.Count} 筆結果",
                    SearchResults = searchResultList,
                    Pagination = new PaginationInfo
                    {
                        CurrentPage = normalizedPage,
                        PageSize = normalizedPageSize,
                        TotalCount = resultList.Count,
                        TotalPages = (int)Math.Ceiling((double)resultList.Count / normalizedPageSize)
                    }
                };

                LogRequestComplete("搜尋人員資料", resultList.Count);
                return Ok(response);
            }, "搜尋人員資料");
        }

        /// <summary>
        /// 獲取我的最愛列表
        /// </summary>
        [HttpGet("favorites")]
        public async Task<IActionResult> GetFavorites()
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                var (userId, userRole) = GetUserInfo();
                
                LogRequestStart("獲取我的最愛", new { UserId = userId });

                var favorites = await _dataAccessService.GetFavoritesAsync(userId);

                var response = new FavoritesResponse
                {
                    Success = true,
                    Message = ApplicationConstants.ApiResponse.SuccessMessages.DataRetrievedSuccessfully,
                    Favorites = favorites.ToList()
                };

                LogRequestComplete("獲取我的最愛", favorites.Count());
                return Ok(response);
            }, "獲取我的最愛");
        }

        /// <summary>
        /// 新增我的最愛
        /// </summary>
        [HttpPost("favorites/{personId}")]
        public async Task<IActionResult> AddFavorite(int personId)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                var (userId, userRole) = GetUserInfo();
                
                LogRequestStart("新增我的最愛", new { UserId = userId, PersonId = personId });

                // 檢查人員是否存在
                var person = await _dataAccessService.GetPersonDataByIdAsync(personId, userId, userRole);
                if (person == null)
                {
                    return CreateNotFoundResponse("人員資料", personId);
                }

                var success = await _dataAccessService.AddFavoriteAsync(userId, personId);

                var response = new ApiResponse
                {
                    Success = success,
                    Message = success 
                        ? ApplicationConstants.ApiResponse.SuccessMessages.DataCreatedSuccessfully 
                        : "該人員已在我的最愛中"
                };

                LogRequestComplete("新增我的最愛");
                return Ok(response);
            }, "新增我的最愛");
        }

        /// <summary>
        /// 移除我的最愛
        /// </summary>
        [HttpDelete("favorites/{personId}")]
        public async Task<IActionResult> RemoveFavorite(int personId)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                var (userId, userRole) = GetUserInfo();
                
                LogRequestStart("移除我的最愛", new { UserId = userId, PersonId = personId });

                var success = await _dataAccessService.RemoveFavoriteAsync(userId, personId);

                var response = new ApiResponse
                {
                    Success = success,
                    Message = success 
                        ? ApplicationConstants.ApiResponse.SuccessMessages.DataDeletedSuccessfully 
                        : "該人員不在我的最愛中"
                };

                LogRequestComplete("移除我的最愛");
                return Ok(response);
            }, "移除我的最愛");
        }

        #region 私有輔助方法

        /// <summary>
        /// 驗證人員資料
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
    /// 我的最愛回應
    /// </summary>
    public class FavoritesResponse : ApiResponse
    {
        public List<FavoriteModel> Favorites { get; set; } = new();
    }

    #endregion
}