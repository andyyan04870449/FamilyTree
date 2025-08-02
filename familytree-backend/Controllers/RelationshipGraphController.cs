// 關聯圖譜控制器：提供人員關聯關係分析功能
// 主要功能：分析所有人員關係、分析選定人員關係、生成圖譜數據
// 設計改善：使用統一的資料存取服務，移除重複代碼，改善架構設計

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using familytree_backend.Models;
using familytree_backend.Services;
using Dapper;
using Npgsql;

namespace familytree_backend.Controllers
{
    /// <summary>
    /// 關聯圖譜控制器
    /// 職責：提供人員關聯關係分析、圖譜生成等功能
    /// 設計改善：使用統一的資料存取服務，移除重複的 SQL 查詢邏輯
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RelationshipGraphController : BaseController
    {
        private readonly IDataAccessService _dataAccessService;

        /// <summary>
        /// 關聯圖譜控制器建構子
        /// 設計改善：使用統一的資料存取服務，避免直接操作資料庫
        /// </summary>
        public RelationshipGraphController(
            ILogger<RelationshipGraphController> logger,
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
        /// 分析所有人員的關聯關係
        /// 設計改善：使用統一的資料存取服務，簡化查詢邏輯
        /// </summary>
        /// <param name="project_id">專案 ID</param>
        /// <returns>關聯圖譜數據</returns>
        [HttpPost("analyze-all")]
        public async Task<IActionResult> AnalyzeAllPersons([FromQuery] string? project_id = null)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("分析所有人員關聯關係", new { ProjectId = project_id });

                // 驗證專案 ID（如果提供）
                if (!string.IsNullOrEmpty(project_id))
                {
                    var projectValidationResult = ValidateProjectId(project_id, allowNull: false);
                    if (projectValidationResult != null)
                    {
                        return projectValidationResult;
                    }
                }

                // 使用統一的資料存取服務獲取人員資料
                var (persons, totalCount) = await _dataAccessService.GetPersonDataListAsync(
                    project_id!, 1, int.MaxValue);

                Logger.LogInformation("獲取人員資料成功，共 {Count} 筆", totalCount);

                // 生成圖譜數據
                var graphData = await GenerateGraphDataAsync(persons.ToList());

                Logger.LogInformation("關聯圖譜生成成功，節點：{Nodes}，連線：{Links}", 
                    graphData.Nodes.Count, graphData.Links.Count);

                var response = new AnalysisResponse
                {
                    Success = true,
                    Message = "關聯分析完成",
                    Data = graphData
                };

                LogRequestComplete("分析所有人員關聯關係");
                return Ok(response);
            }, "分析所有人員關聯關係");
        }

        /// <summary>
        /// 建立人員關係
        /// 設計改善：使用統一的資料存取服務，簡化關係建立邏輯
        /// </summary>
        /// <param name="request">建立關係請求</param>
        /// <returns>建立結果</returns>
        [HttpPost("create-relationship")]
        public async Task<IActionResult> CreateRelationship([FromBody] CreateRelationshipRequest request)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("建立人員關係", new { 
                    SourcePersonId = request.SourcePersonId, 
                    TargetPersonId = request.TargetPersonId,
                    RelationshipType = request.RelationshipType
                });

                // 參數驗證
                if (request == null)
                {
                    return CreateErrorResponse("請求物件不能為空");
                }

                if (request.SourcePersonId <= 0 || request.TargetPersonId <= 0)
                {
                    return CreateErrorResponse("人員 ID 必須大於 0");
                }

                if (request.SourcePersonId == request.TargetPersonId)
                {
                    return CreateErrorResponse("來源人員和目標人員不能相同");
                }

                if (string.IsNullOrWhiteSpace(request.RelationshipType))
                {
                    return CreateErrorResponse("關係類型不能為空");
                }

                // 從視覺化分析圖表獲取專案ID
                string? projectId = null;
                if (request.VisualAnalysisGraphId.HasValue)
                {
                    using var connection = new NpgsqlConnection(ConfigurationService.GetConnectionString());
                    await connection.OpenAsync();
                    
                    var graphSql = "SELECT project_ids FROM visual_analysis_graphs WHERE id = @id";
                    var graphResult = await connection.QuerySingleOrDefaultAsync<string>(graphSql, new { id = request.VisualAnalysisGraphId.Value });
                    
                    if (!string.IsNullOrEmpty(graphResult))
                    {
                        // 取第一個專案ID（假設只有一個專案）
                        var projectIds = graphResult.Split(',').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToList();
                        if (projectIds.Any())
                        {
                            projectId = projectIds.First();
                        }
                    }
                }

                // 建立關係資料
                var relationship = new RelationshipData
                {
                    SourcePersonId = request.SourcePersonId,
                    TargetPersonId = request.TargetPersonId,
                    RelationshipType = request.RelationshipType,
                    ProjectId = projectId,
                    VisualAnalysisGraphId = request.VisualAnalysisGraphId,
                    CreatedAt = DateTime.UtcNow
                };

                // 使用統一的資料存取服務建立關係
                var success = await _dataAccessService.CreateRelationshipAsync(relationship);

                if (!success)
                {
                    return CreateErrorResponse("建立關係失敗");
                }

                Logger.LogInformation("成功建立人員關係：{SourceId} -> {TargetId} ({Type})", 
                    request.SourcePersonId, request.TargetPersonId, request.RelationshipType);

                var response = new ApiResponse
                {
                    Success = true,
                    Message = "關係建立成功"
                };

                LogRequestComplete("建立人員關係");
                return Ok(response);
            }, "建立人員關係");
        }

        /// <summary>
        /// 分析選定人員的關聯關係
        /// 設計改善：使用統一的資料存取服務，簡化選定人員分析邏輯
        /// </summary>
        /// <param name="request">分析請求</param>
        /// <param name="project_id">專案 ID</param>
        /// <returns>關聯圖譜數據</returns>
        [HttpPost("analyze-selected")]
        public async Task<IActionResult> AnalyzeSelectedPersons([FromBody] RelationshipAnalysisRequest request, [FromQuery] string? project_id = null)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("分析選定人員關聯關係", new { 
                    PersonIds = request.PersonIds, 
                    AnalysisType = request.AnalysisType,
                    MaxDepth = request.MaxDepth,
                    ProjectId = project_id
                });

                // 參數驗證
                if (request == null || request.PersonIds == null || !request.PersonIds.Any())
                {
                    return CreateErrorResponse("請選擇要分析的人員");
                }

                // 驗證專案 ID（如果提供）
                if (!string.IsNullOrEmpty(project_id))
                {
                    var projectValidationResult = ValidateProjectId(project_id, allowNull: false);
                    if (projectValidationResult != null)
                    {
                        return projectValidationResult;
                    }
                }

                // 使用統一的資料存取服務獲取選定人員資料
                var selectedPersons = new List<PersonDataModel>();
                foreach (var personId in request.PersonIds)
                {
                    var person = await _dataAccessService.GetPersonDataByIdAsync(personId, project_id!);
                    if (person != null)
                    {
                        selectedPersons.Add(person);
                    }
                }

                if (!selectedPersons.Any())
                {
                    return CreateErrorResponse("未找到指定的人員資料");
                }

                Logger.LogInformation("獲取選定人員資料成功，共 {Count} 筆", selectedPersons.Count);

                // 生成圖譜數據
                var graphData = await GenerateGraphDataAsync(selectedPersons, request.MaxDepth);

                Logger.LogInformation("選定人員關聯圖譜生成成功，節點：{Nodes}，連線：{Links}", 
                    graphData.Nodes.Count, graphData.Links.Count);

                var response = new AnalysisResponse
                {
                    Success = true,
                    Message = "選定人員關聯分析完成",
                    Data = graphData
                };

                LogRequestComplete("分析選定人員關聯關係");
                return Ok(response);
            }, "分析選定人員關聯關係");
        }





        #region 私有輔助方法

        /// <summary>
        /// 生成圖譜數據
        /// 設計理念：統一的圖譜數據生成邏輯
        /// </summary>
        private GraphData GenerateGraphData(List<PersonDataModel> persons)
        {
            var nodes = new List<GraphNode>();
            var links = new List<GraphLink>();

            // 建立節點
            foreach (var person in persons)
            {
                nodes.Add(new GraphNode
                {
                    Id = person.Id.ToString(),
                    Name = person.Name,
                    Gender = person.Gender ?? "",
                    Photo = person.Photo,
                    IsExpanded = true
                });
            }

            // 建立連線
            foreach (var person in persons)
            {
                // 解析家庭關係
                var familyLinks = ParseFamilyRelationships(person, persons);
                links.AddRange(familyLinks);

                // 解析朋友關係
                var friendLinks = ParseFriendRelationships(person, persons);
                links.AddRange(friendLinks);
            }

            // 移除重複連線
            links = RemoveDuplicateLinks(links);

            return new GraphData
            {
                Nodes = nodes,
                Links = links,
                Metadata = new GraphMetadata
                {
                    TotalNodes = nodes.Count,
                    TotalLinks = links.Count,
                    FamilyLinks = links.Count(l => l.IsFamily),
                    FriendLinks = links.Count(l => !l.IsFamily),
                    AnalysisDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                }
            };
        }

        /// <summary>
        /// 生成圖譜數據（非同步版本）
        /// </summary>
        private async Task<GraphData> GenerateGraphDataAsync(List<PersonDataModel> persons, int? maxDepth = null)
        {
            return await Task.Run(() => GenerateGraphData(persons));
        }

        /// <summary>
        /// 解析家庭關係
        /// </summary>
        private List<GraphLink> ParseFamilyRelationships(PersonDataModel person, List<PersonDataModel> allPersons)
        {
            var links = new List<GraphLink>();

            if (string.IsNullOrWhiteSpace(person.FamilyRelationships))
                return links;

            // 簡單的關係解析邏輯
            var relationships = person.FamilyRelationships.Split(',', ';', '，', '；');
            foreach (var relationship in relationships)
            {
                var trimmed = relationship.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;

                // 尋找匹配的人員
                var targetPerson = allPersons.FirstOrDefault(p => 
                    p.Name.Contains(trimmed) || trimmed.Contains(p.Name));

                if (targetPerson != null && targetPerson.Id != person.Id)
                {
                    links.Add(new GraphLink
                    {
                        Source = person.Id.ToString(),
                        Target = targetPerson.Id.ToString(),
                        Type = "family",
                        IsFamily = true,
                        Strength = 1.0
                    });
                }
            }

            return links;
        }

        /// <summary>
        /// 解析朋友關係
        /// </summary>
        private List<GraphLink> ParseFriendRelationships(PersonDataModel person, List<PersonDataModel> allPersons)
        {
            var links = new List<GraphLink>();

            if (string.IsNullOrWhiteSpace(person.ImportantFriends))
                return links;

            // 簡單的關係解析邏輯
            var friends = person.ImportantFriends.Split(',', ';', '，', '；');
            foreach (var friend in friends)
            {
                var trimmed = friend.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;

                // 尋找匹配的人員
                var targetPerson = allPersons.FirstOrDefault(p => 
                    p.Name.Contains(trimmed) || trimmed.Contains(p.Name));

                if (targetPerson != null && targetPerson.Id != person.Id)
                {
                    links.Add(new GraphLink
                    {
                        Source = person.Id.ToString(),
                        Target = targetPerson.Id.ToString(),
                        Type = "friend",
                        IsFamily = false,
                        Strength = 0.8
                    });
                }
            }

            return links;
        }

        /// <summary>
        /// 移除重複連線
        /// </summary>
        private List<GraphLink> RemoveDuplicateLinks(List<GraphLink> links)
        {
            var uniqueLinks = new List<GraphLink>();
            var seenPairs = new HashSet<string>();

            foreach (var link in links)
            {
                var pair1 = $"{link.Source}-{link.Target}";
                var pair2 = $"{link.Target}-{link.Source}";

                if (!seenPairs.Contains(pair1) && !seenPairs.Contains(pair2))
                {
                    uniqueLinks.Add(link);
                    seenPairs.Add(pair1);
                    seenPairs.Add(pair2);
                }
            }

            return uniqueLinks;
        }

        #endregion
    }

    #region 回應模型

    /// <summary>
    /// 關係分析請求
    /// </summary>
    public class RelationshipAnalysisRequest
    {
        public List<int> PersonIds { get; set; } = new();
        public string AnalysisType { get; set; } = "selected";
        public int? MaxDepth { get; set; }
    }

    /// <summary>
    /// 分析回應
    /// </summary>
    public class AnalysisResponse : ApiResponse
    {
        public GraphData? Data { get; set; }
    }

    /// <summary>
    /// 圖譜節點
    /// </summary>
    public class GraphNode
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Gender { get; set; } = "";
        public string? Photo { get; set; }
        public bool IsExpanded { get; set; }
        public double? X { get; set; }
        public double? Y { get; set; }
        public double? Fx { get; set; }
        public double? Fy { get; set; }
        public object? Data { get; set; }
    }

    /// <summary>
    /// 圖譜連線
    /// </summary>
    public class GraphLink
    {
        public string Source { get; set; } = "";
        public string Target { get; set; } = "";
        public string Type { get; set; } = "";
        public bool IsFamily { get; set; }
        public double? Strength { get; set; }
    }

    /// <summary>
    /// 圖譜元資料
    /// </summary>
    public class GraphMetadata
    {
        public int TotalNodes { get; set; }
        public int TotalLinks { get; set; }
        public int FamilyLinks { get; set; }
        public int FriendLinks { get; set; }
        public string AnalysisDate { get; set; } = "";
    }

    /// <summary>
    /// 圖譜數據
    /// </summary>
    public class GraphData
    {
        public List<GraphNode> Nodes { get; set; } = new();
        public List<GraphLink> Links { get; set; } = new();
        public GraphMetadata? Metadata { get; set; }
    }

    /// <summary>
    /// 建立關係請求
    /// </summary>
    public class CreateRelationshipRequest
    {
        public int SourcePersonId { get; set; }
        public int TargetPersonId { get; set; }
        public string RelationshipType { get; set; } = "";
        public int? VisualAnalysisGraphId { get; set; }
    }

    #endregion
} 