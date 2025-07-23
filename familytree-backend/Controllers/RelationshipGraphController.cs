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
        public async Task<IActionResult> AnalyzeAllPersons([FromQuery] string? project_id = null)
        {
            _logger.LogInformation("📊 開始分析所有人員關聯關係 - 專案ID: {ProjectId}", project_id);
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
                    WHERE (@project_id IS NULL OR project_id = @project_id)
                    ORDER BY name", new { project_id });

                _logger.LogInformation("✅ 獲取人員資料成功，共 {count} 筆", persons.Count());
                _logger.LogInformation("🔍 人員詳情: {persons}", string.Join(", ", persons.Select(p => $"{p.Id}:{p.Name}")));

                // 生成圖譜數據
                _logger.LogInformation("🔍 準備調用 GenerateGraphDataAsync 方法");
                var graphData = await GenerateGraphDataAsync(persons.ToList());
                _logger.LogInformation("🔍 GenerateGraphDataAsync 方法調用完成");

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
        /// 建立人員關係
        /// </summary>
        /// <param name="request">建立關係請求</param>
        /// <returns>建立結果</returns>
        [HttpPost("create-relationship")]
        public async Task<IActionResult> CreateRelationship([FromBody] CreateRelationshipRequest request)
        {
            _logger.LogInformation("🔗 開始建立人員關係");
            _logger.LogInformation("🔍 關係詳情: {sourceId} -> {targetId}, 類型: {type}", 
                request.SourcePersonId, request.TargetPersonId, request.RelationshipType);

            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // 檢查人員是否存在
                var sourcePerson = await connection.QueryFirstOrDefaultAsync<PersonDataModel>(
                    "SELECT id, name FROM person_profile WHERE id = @Id", 
                    new { Id = request.SourcePersonId });

                var targetPerson = await connection.QueryFirstOrDefaultAsync<PersonDataModel>(
                    "SELECT id, name FROM person_profile WHERE id = @Id", 
                    new { Id = request.TargetPersonId });

                if (sourcePerson == null)
                {
                    _logger.LogWarning("❌ 來源人員不存在: {sourceId}", request.SourcePersonId);
                    return BadRequest(new { Success = false, Message = "來源人員不存在" });
                }

                if (targetPerson == null)
                {
                    _logger.LogWarning("❌ 目標人員不存在: {targetId}", request.TargetPersonId);
                    return BadRequest(new { Success = false, Message = "目標人員不存在" });
                }

                // 檢查關係是否已存在
                var existingRelationship = await connection.QueryFirstOrDefaultAsync(
                    @"SELECT id FROM relationship_layers 
                      WHERE source_person_id = @SourceId AND target_person_id = @TargetId 
                      AND relation_type = @Type",
                    new { SourceId = request.SourcePersonId, TargetId = request.TargetPersonId, Type = request.RelationshipType });

                if (existingRelationship != null)
                {
                    _logger.LogWarning("❌ 關係已存在: {sourceId} -> {targetId}, 類型: {type}", 
                        request.SourcePersonId, request.TargetPersonId, request.RelationshipType);
                    return BadRequest(new { Success = false, Message = "此關係已存在" });
                }

                // 生成分析會話ID
                var sessionId = $"manual_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}";

                // 插入關係資料
                var insertSql = @"
                    INSERT INTO relationship_layers 
                    (source_person_id, target_person_id, relation_type, source_field, layer_depth, analysis_session_id)
                    VALUES (@SourceId, @TargetId, @Type, 'manual', 1, @SessionId)";

                await connection.ExecuteAsync(insertSql, new
                {
                    SourceId = request.SourcePersonId,
                    TargetId = request.TargetPersonId,
                    Type = request.RelationshipType,
                    SessionId = sessionId
                });

                _logger.LogInformation("✅ 關係建立成功: {sourceName} -> {targetName}, 類型: {type}", 
                    sourcePerson.Name, targetPerson.Name, request.RelationshipType);

                return Ok(new { Success = true, Message = "關係建立成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 建立關係失敗");
                return StatusCode(500, new { Success = false, Message = "建立關係失敗：" + ex.Message });
            }
        }

        /// <summary>
        /// 分析選定人員的關聯關係
        /// </summary>
        /// <param name="request">分析請求</param>
        /// <returns>關聯圖譜數據</returns>
        [HttpPost("analyze-selected")]
        public async Task<IActionResult> AnalyzeSelectedPersons([FromBody] RelationshipAnalysisRequest request, [FromQuery] string? project_id = null)
        {
            _logger.LogInformation("📊 開始分析選定人員關聯關係，人員數量：{count}，專案ID: {ProjectId}", request.PersonIds.Count, project_id);

            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // 獲取選定人員及其相關人員
                var personIds = string.Join(",", request.PersonIds);
                var maxDepth = request.MaxDepth ?? 3;

                _logger.LogInformation("🔍 開始遞迴查找相關人員，初始人員ID: {personIds}, 最大深度: {maxDepth}", 
                    string.Join(",", request.PersonIds), maxDepth);

                // 獲取選定人員及其相關人員（包括手動建立的關係）
                var persons = await connection.QueryAsync<PersonDataModel>(@"
                    WITH related_persons AS (
                        -- 選定的人員
                        SELECT id FROM person_profile WHERE id = ANY(@PersonIds) AND (@project_id IS NULL OR project_id = @project_id)
                        UNION
                        -- 通過家族關係和朋友關係相關的人員
                        SELECT DISTINCT p.id
                        FROM person_profile p
                        WHERE (@project_id IS NULL OR p.project_id = @project_id)
                        AND EXISTS (
                            SELECT 1 FROM person_profile pp 
                            WHERE pp.id = ANY(@PersonIds) AND (@project_id IS NULL OR pp.project_id = @project_id)
                            AND (
                                -- 家族關係檢查
                                (pp.family_relationships IS NOT NULL AND pp.family_relationships LIKE '%' || p.name || '%') OR
                                (p.family_relationships IS NOT NULL AND p.family_relationships LIKE '%' || pp.name || '%') OR
                                -- 朋友關係檢查
                                (pp.important_friends IS NOT NULL AND pp.important_friends LIKE '%' || p.name || '%') OR
                                (p.important_friends IS NOT NULL AND p.important_friends LIKE '%' || pp.name || '%')
                            )
                        )
                        UNION
                        -- 手動建立的關係中的相關人員
                        SELECT DISTINCT rl.target_person_id
                        FROM relationship_layers rl
                        WHERE rl.source_person_id = ANY(@PersonIds)
                        UNION
                        SELECT DISTINCT rl.source_person_id
                        FROM relationship_layers rl
                        WHERE rl.target_person_id = ANY(@PersonIds)
                    )
                    SELECT DISTINCT 
                        pp.id, pp.name, pp.gender, pp.birthday, pp.mobile,
                        pp.family_relationships, pp.important_friends, pp.created_at, pp.updated_at
                    FROM person_profile pp
                    INNER JOIN related_persons rp ON pp.id = rp.id
                    WHERE (@project_id IS NULL OR pp.project_id = @project_id)
                    ORDER BY pp.name", new { PersonIds = request.PersonIds, project_id });

                _logger.LogInformation("✅ 獲取相關人員資料成功，共 {count} 筆", persons.Count());

                // 生成圖譜數據
                var graphData = await GenerateGraphDataAsync(persons.ToList());

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
            return GenerateGraphDataAsync(persons).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 生成圖譜數據（異步版本，包含手動建立的關係）
        /// </summary>
        private async Task<GraphData> GenerateGraphDataAsync(List<PersonDataModel> persons)
        {
            try
            {
                _logger.LogInformation("🔍 開始生成圖譜數據，人員數量：{count}", persons.Count());
                
                // 檢查人員資料的完整性
                foreach (var person in persons)
                {
                    _logger.LogDebug("🔍 檢查人員資料: ID={id}, Name={name}, FamilyRelationships={family}, ImportantFriends={friends}", 
                        person.Id, person.Name, 
                        person.FamilyRelationships ?? "null", 
                        person.ImportantFriends ?? "null");
                }
                
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

            // 獲取手動建立的關係
            _logger.LogInformation("🔍 開始獲取手動建立的關係");
            var manualRelationships = new List<GraphLink>();
            
            if (persons.Any())
            {
                var personIds = persons.Select(p => p.Id).ToList();
                _logger.LogInformation("🔍 人員ID列表包含 {count} 個ID，前10個: {ids}", personIds.Count, string.Join(", ", personIds.Take(10)));
                _logger.LogInformation("🔍 檢查特定ID是否存在: 266={has266}, 234={has234}, 219={has219}, 206={has206}", 
                    personIds.Contains(266), personIds.Contains(234), personIds.Contains(219), personIds.Contains(206));
                
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                // 先檢查資料庫中所有手動關係
                var allManualLinks = await connection.QueryAsync(@"
                    SELECT 
                        source_person_id as SourcePersonId,
                        target_person_id as TargetPersonId,
                        relation_type as RelationType
                    FROM relationship_layers 
                    WHERE source_field = 'manual'");
                
                _logger.LogInformation("🔍 資料庫中所有手動關係: {count} 筆", allManualLinks.Count());
                
                // 使用字符串拼接的方式構建 IN 子句
                var personIdsString = string.Join(",", personIds);
                _logger.LogInformation("🔍 構建的 SQL IN 子句: {sql}", $"source_person_id IN ({personIdsString}) AND target_person_id IN ({personIdsString})");
                
                var manualLinks = await connection.QueryAsync($@"
                    SELECT 
                        source_person_id as SourcePersonId,
                        target_person_id as TargetPersonId,
                        relation_type as RelationType
                    FROM relationship_layers 
                    WHERE source_person_id IN ({personIdsString}) AND target_person_id IN ({personIdsString})
                    AND source_field = 'manual'");
                
                _logger.LogInformation("🔍 查詢結果數量: {count}", manualLinks.Count());
                
                                foreach (dynamic link in manualLinks)
                {
                    // 使用更安全的方式訪問動態屬性
                    var sourceId = ((IDictionary<string, object>)link)["sourcepersonid"];
                    var targetId = ((IDictionary<string, object>)link)["targetpersonid"];
                    var relationType = ((IDictionary<string, object>)link)["relationtype"];
                    
                    // 安全檢查 null 值
                    if (sourceId == null || targetId == null || relationType == null)
                    {
                        _logger.LogWarning("🔍 跳過 null 值的手動關係");
                        continue;
                    }
                    
                    manualRelationships.Add(new GraphLink
                    {
                        Source = sourceId.ToString(),
                        Target = targetId.ToString(),
                        Type = relationType.ToString(),
                        IsFamily = false, // 手動建立的關係預設為非家族關係
                        Strength = 1.0
                    });
                    
                    _logger.LogInformation("🔍 成功添加手動關係: {source} -> {target} ({type})", 
                        sourceId.ToString(), targetId.ToString(), relationType.ToString());
                }
                
                _logger.LogInformation("🔍 手動建立的關係數量：{count}", manualRelationships.Count);
            }

            // 合併所有關係
            links.AddRange(manualRelationships);
            
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 生成圖譜數據時發生錯誤: {message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// 解析家族關係
        /// </summary>
        private List<GraphLink> ParseFamilyRelationships(PersonDataModel person, List<PersonDataModel> allPersons)
        {
            var links = new List<GraphLink>();
            
            // 安全檢查 null 或空值
            if (string.IsNullOrEmpty(person.FamilyRelationships))
            {
                _logger.LogDebug("🔍 人員 {name} 沒有家族關係資料", person.Name);
                return links;
            }
            
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
            
            // 安全檢查 null 或空值
            if (string.IsNullOrEmpty(person.ImportantFriends))
            {
                _logger.LogDebug("🔍 人員 {name} 沒有朋友關係資料", person.Name);
                return links;
            }
            
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

    public class CreateRelationshipRequest
    {
        public int SourcePersonId { get; set; }
        public int TargetPersonId { get; set; }
        public string RelationshipType { get; set; } = "";
    }
} 