// 關聯圖譜控制器：提供人員關聯關係分析功能
// 主要功能：分析所有人員關係、分析選定人員關係、生成圖譜數據

using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Dapper;
using System.Text.Json;
using familytree_backend.Models;

namespace familytree_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RelationshipGraphController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly ILogger<RelationshipGraphController> _logger;

        public RelationshipGraphController(IConfiguration configuration, ILogger<RelationshipGraphController> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
            _logger = logger;
        }

        /// <summary>
        /// 分析所有人員的關聯關係
        /// </summary>
        /// <returns>關聯圖譜數據</returns>
        [HttpPost("analyze-all")]
        public async Task<IActionResult> AnalyzeAllPersons()
        {
            _logger.LogInformation("📊 開始分析所有人員關聯關係");
            _logger.LogInformation("🔍 請求時間: {RequestTime}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // 獲取所有人員資料
                var persons = await connection.QueryAsync<PersonDataModel>(@"
                    SELECT 
                        id,
                        name,
                        gender,
                        birthday,
                        mobile,
                        family_relationships,
                        important_friends,
                        created_at,
                        updated_at
                    FROM person_profile 
                    ORDER BY name");

                _logger.LogInformation("✅ 獲取人員資料成功，共 {count} 筆", persons.Count());
                _logger.LogInformation("🔍 人員詳情: {persons}", string.Join(", ", persons.Select(p => $"{p.Id}:{p.Name}")));

                // 生成圖譜數據
                _logger.LogInformation("🔍 準備調用 GenerateGraphData 方法");
                var graphData = GenerateGraphData(persons.ToList());
                _logger.LogInformation("🔍 GenerateGraphData 方法調用完成");

                _logger.LogInformation("✅ 關聯圖譜生成成功，節點：{nodes}，連線：{links}", 
                    graphData.Nodes.Count, graphData.Links.Count);

                return Ok(new AnalysisResponse
                {
                    Success = true,
                    Message = "關聯分析完成",
                    Data = graphData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 分析所有人員關聯關係失敗");
                return StatusCode(500, new AnalysisResponse
                {
                    Success = false,
                    Message = "分析失敗：" + ex.Message
                });
            }
        }

        /// <summary>
        /// 分析選定人員的關聯關係
        /// </summary>
        /// <param name="request">分析請求</param>
        /// <returns>關聯圖譜數據</returns>
        [HttpPost("analyze-selected")]
        public async Task<IActionResult> AnalyzeSelectedPersons([FromBody] RelationshipAnalysisRequest request)
        {
            _logger.LogInformation("📊 開始分析選定人員關聯關係，人員數量：{count}", request.PersonIds.Count);

            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // 獲取選定人員及其相關人員
                var personIds = string.Join(",", request.PersonIds);
                var maxDepth = request.MaxDepth ?? 3;

                var persons = await connection.QueryAsync<PersonDataModel>(@"
                    WITH RECURSIVE related_persons AS (
                        -- 初始人員
                        SELECT 
                            id, name, gender, birthday, mobile, 
                            family_relationships, important_friends, created_at, updated_at,
                            0 as depth
                        FROM person_profile 
                        WHERE id = ANY(@PersonIds)
                        
                        UNION ALL
                        
                        -- 遞迴查找相關人員
                        SELECT 
                            p.id, p.name, p.gender, p.birthday, p.mobile,
                            p.family_relationships, p.important_friends, p.created_at, p.updated_at,
                            rp.depth + 1
                        FROM person_profile p
                        INNER JOIN related_persons rp ON (
                            -- 家族關係
                            (rp.family_relationships LIKE '%' || p.name || '%') OR
                            (p.family_relationships LIKE '%' || rp.name || '%') OR
                            -- 朋友關係
                            (rp.important_friends LIKE '%' || p.name || '%') OR
                            (p.important_friends LIKE '%' || rp.name || '%')
                        )
                        WHERE rp.depth < @MaxDepth
                    )
                    SELECT DISTINCT 
                        id, name, gender, birthday, mobile,
                        family_relationships, important_friends, created_at, updated_at
                    FROM related_persons
                    ORDER BY name", new { PersonIds = request.PersonIds, MaxDepth = maxDepth });

                _logger.LogInformation("✅ 獲取相關人員資料成功，共 {count} 筆", persons.Count());

                // 生成圖譜數據
                var graphData = GenerateGraphData(persons.ToList());

                _logger.LogInformation("✅ 選定人員關聯圖譜生成成功，節點：{nodes}，連線：{links}", 
                    graphData.Nodes.Count, graphData.Links.Count);

                return Ok(new AnalysisResponse
                {
                    Success = true,
                    Message = "選定人員關聯分析完成",
                    Data = graphData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 分析選定人員關聯關係失敗");
                return StatusCode(500, new AnalysisResponse
                {
                    Success = false,
                    Message = "分析失敗：" + ex.Message
                });
            }
        }

        /// <summary>
        /// 生成圖譜數據
        /// </summary>
        private GraphData GenerateGraphData(List<PersonDataModel> persons)
        {
            _logger.LogInformation("🔍 開始生成圖譜數據，人員數量：{count}", persons.Count());
            
            var nodes = persons.Select(p => new GraphNode
            {
                Id = p.Id.ToString(),
                Name = p.Name,
                Gender = p.Gender == "男" ? "male" : "female",
                Photo = null,
                IsExpanded = true,
                                    Data = new
                    {
                        p.Id,
                        p.Name,
                        p.Gender,
                        p.Birthday,
                        p.Mobile,
                        p.FamilyRelationships,
                        p.ImportantFriends,
                        p.CreatedAt,
                        p.UpdatedAt
                    }
            }).ToList();

            var links = new List<GraphLink>();

            // 解析家族關係
            _logger.LogInformation("🔍 開始解析家族關係");
            var familyRelationshipsCount = 0;
            foreach (var person in persons)
            {
                if (!string.IsNullOrEmpty(person.FamilyRelationships))
                {
                    familyRelationshipsCount++;
                    var familyLinks = ParseFamilyRelationships(person, persons);
                    links.AddRange(familyLinks);
                    _logger.LogInformation("🔍 人員 {name} 的家族關係：{relationships}", person.Name, person.FamilyRelationships);
                }
            }
            _logger.LogInformation("🔍 家族關係解析完成，有關係的人員：{count}", familyRelationshipsCount);

            // 解析朋友關係
            _logger.LogInformation("🔍 開始解析朋友關係");
            var friendRelationshipsCount = 0;
            foreach (var person in persons)
            {
                if (!string.IsNullOrEmpty(person.ImportantFriends))
                {
                    friendRelationshipsCount++;
                    var friendLinks = ParseFriendRelationships(person, persons);
                    links.AddRange(friendLinks);
                    _logger.LogInformation("🔍 人員 {name} 的朋友關係：{friends}", person.Name, person.ImportantFriends);
                }
            }
            _logger.LogInformation("🔍 朋友關係解析完成，有關係的人員：{count}", friendRelationshipsCount);

            // 移除重複連線
            _logger.LogInformation("🔍 移除重複連線前，總連線數：{count}", links.Count);
            var uniqueLinks = RemoveDuplicateLinks(links);
            _logger.LogInformation("🔍 移除重複連線後，總連線數：{count}", uniqueLinks.Count);

            var metadata = new GraphMetadata
            {
                TotalNodes = nodes.Count,
                TotalLinks = uniqueLinks.Count,
                FamilyLinks = uniqueLinks.Count(l => l.IsFamily),
                FriendLinks = uniqueLinks.Count(l => !l.IsFamily),
                AnalysisDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            var result = new GraphData
            {
                Nodes = nodes,
                Links = uniqueLinks,
                Metadata = metadata
            };

            _logger.LogInformation("✅ 圖譜數據生成完成，節點：{nodes}，連線：{links}，家族連線：{familyLinks}，朋友連線：{friendLinks}", 
                result.Nodes.Count, result.Links.Count, result.Metadata.FamilyLinks, result.Metadata.FriendLinks);

            return result;
        }

        /// <summary>
        /// 解析家族關係
        /// </summary>
        private List<GraphLink> ParseFamilyRelationships(PersonDataModel person, List<PersonDataModel> allPersons)
        {
            var links = new List<GraphLink>();
            var lines = person.FamilyRelationships.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var parts = line.Split('：');
                if (parts.Length == 2)
                {
                    var relationType = parts[0].Trim();
                    var targetNames = parts[1].Split(new[] { ',', '，', '、' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var targetName in targetNames)
                    {
                        var trimmedName = targetName.Trim();
                        if (!string.IsNullOrEmpty(trimmedName))
                        {
                            var targetPerson = allPersons.FirstOrDefault(p => p.Name == trimmedName);
                            if (targetPerson != null)
                            {
                                links.Add(new GraphLink
                                {
                                    Source = person.Id.ToString(),
                                    Target = targetPerson.Id.ToString(),
                                    Type = relationType,
                                    IsFamily = true,
                                    Strength = 1.0
                                });
                            }
                        }
                    }
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
            var lines = person.ImportantFriends.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var parts = line.Split(new[] { ',', '，', '、' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    var trimmedPart = part.Trim();
                    if (!string.IsNullOrEmpty(trimmedPart))
                    {
                        // 提取姓名（去除可能的額外資訊）
                        var nameMatch = System.Text.RegularExpressions.Regex.Match(trimmedPart, @"^([^\s，、]+)");
                        if (nameMatch.Success)
                        {
                            var name = nameMatch.Groups[1].Value.Trim();
                            if (!string.IsNullOrEmpty(name))
                            {
                                var friendPerson = allPersons.FirstOrDefault(p => p.Name == name);
                                if (friendPerson != null)
                                {
                                    links.Add(new GraphLink
                                    {
                                        Source = person.Id.ToString(),
                                        Target = friendPerson.Id.ToString(),
                                        Type = "朋友",
                                        IsFamily = false,
                                        Strength = 0.5
                                    });
                                }
                            }
                        }
                    }
                }
            }

            return links;
        }

        /// <summary>
        /// 移除重複連線
        /// </summary>
        private List<GraphLink> RemoveDuplicateLinks(List<GraphLink> links)
        {
            return links.Where((link, index) => 
                index == links.FindIndex(l => 
                    (l.Source == link.Source && l.Target == link.Target && l.Type == link.Type) ||
                    (l.Source == link.Target && l.Target == link.Source && l.Type == link.Type)
                )
            ).ToList();
        }
    }

    // 數據模型
    public class RelationshipAnalysisRequest
    {
        public List<int> PersonIds { get; set; } = new();
        public string AnalysisType { get; set; } = "selected";
        public int? MaxDepth { get; set; }
    }

    public class AnalysisResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public GraphData? Data { get; set; }
    }

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

    public class GraphLink
    {
        public string Source { get; set; } = "";
        public string Target { get; set; } = "";
        public string Type { get; set; } = "";
        public bool IsFamily { get; set; }
        public double? Strength { get; set; }
    }

    public class GraphMetadata
    {
        public int TotalNodes { get; set; }
        public int TotalLinks { get; set; }
        public int FamilyLinks { get; set; }
        public int FriendLinks { get; set; }
        public string AnalysisDate { get; set; } = "";
    }

    public class GraphData
    {
        public List<GraphNode> Nodes { get; set; } = new();
        public List<GraphLink> Links { get; set; } = new();
        public GraphMetadata? Metadata { get; set; }
    }
} 