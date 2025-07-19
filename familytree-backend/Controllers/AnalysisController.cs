using Microsoft.AspNetCore.Mvc;
using familytree_backend.Models;
using familytree_backend.Services;
using Npgsql;
using Dapper;

namespace familytree_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnalysisController : ControllerBase
    {
        private readonly AnalysisBackgroundService _analysisService;
        private readonly ILogger<AnalysisController> _logger;
        private readonly IConfiguration _configuration;

        public AnalysisController(
            AnalysisBackgroundService analysisService,
            ILogger<AnalysisController> logger,
            IConfiguration configuration)
        {
            _analysisService = analysisService;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost("start")]
        public async Task<IActionResult> StartAnalysis([FromBody] AnalysisRequest request)
        {
            try
            {
                _logger.LogInformation("=== 收到分析啟動請求 ===");
                _logger.LogInformation("請求參數: PersonId = {PersonId}, MaxDepth = {MaxDepth}", request.PersonId, request.MaxDepth);
                _logger.LogInformation("請求時間: {RequestTime}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                
                var success = await _analysisService.StartAnalysis(request.PersonId, request.MaxDepth);
                
                if (success)
                {
                    _logger.LogInformation("✅ 分析啟動成功: PersonId = {PersonId}, MaxDepth = {MaxDepth}", request.PersonId, request.MaxDepth);
                    return Ok(new { 
                        success = true, 
                        message = $"遞迴分析已開始（最大深度：{request.MaxDepth}層），請稍後查看進度",
                        personId = request.PersonId,
                        maxDepth = request.MaxDepth
                    });
                }
                else
                {
                    _logger.LogWarning("⚠️ 分析啟動失敗: 該人員已有進行中的分析任務, PersonId = {PersonId}", request.PersonId);
                    return BadRequest(new { 
                        success = false, 
                        message = "該人員已有進行中的分析任務" 
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 啟動遞迴分析失敗: PersonId = {PersonId}, MaxDepth = {MaxDepth}", request.PersonId, request.MaxDepth);
                _logger.LogError("錯誤詳情: {ErrorMessage}", ex.Message);
                return StatusCode(500, new { 
                    success = false, 
                    message = "啟動遞迴分析時發生錯誤" 
                });
            }
        }

        [HttpGet("progress/{personId}")]
        public async Task<IActionResult> GetProgress(int personId)
        {
            try
            {
                _logger.LogInformation("=== 收到進度查詢請求 ===");
                _logger.LogInformation("查詢參數: PersonId = {PersonId}", personId);
                _logger.LogInformation("查詢時間: {QueryTime}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                
                var progress = await _analysisService.GetAnalysisProgress(personId);
                
                if (progress == null)
                {
                    _logger.LogWarning("⚠️ 找不到分析記錄: PersonId = {PersonId}", personId);
                    return NotFound(new { 
                        success = false, 
                        message = "找不到該人員的分析記錄" 
                    });
                }

                _logger.LogInformation("✅ 進度查詢成功: PersonId = {PersonId}, Status = {Status}, Progress = {Progress}%", 
                    personId, progress.Status, progress.ProgressPercentage);
                
                return Ok(new { 
                    success = true, 
                    data = progress 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 查詢分析進度失敗: PersonId = {PersonId}", personId);
                _logger.LogError("錯誤詳情: {ErrorMessage}", ex.Message);
                return StatusCode(500, new { 
                    success = false, 
                    message = "查詢進度時發生錯誤" 
                });
            }
        }



        [HttpGet("jobs")]
        public async Task<IActionResult> GetAnalysisJobs()
        {
            try
            {
                using var connection = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                await connection.OpenAsync();
                
                // 獲取傳統分析記錄
                var traditionalJobs = await connection.QueryAsync(
                    @"SELECT ar.person_id AS personid, 
                             p.name AS personname,
                             ar.status AS status, 
                             ar.progress_percentage AS progresspercentage, 
                             ar.created_at AS starttime, 
                             CASE WHEN ar.status = 'completed' THEN ar.updated_at ELSE NULL END AS completedtime, 
                             ar.current_step AS currentstep,
                             ar.status_message AS statusmessage,
                             'traditional' AS analysistype
                      FROM analysis_results ar
                      LEFT JOIN person_profile p ON ar.person_id = p.id
                      WHERE ar.status IN ('processing', 'completed', 'failed')");

                // 獲取遞迴分析記錄
                var recursiveJobs = await connection.QueryAsync(
                    @"SELECT s.root_person_id AS personid, 
                             p.name AS personname,
                             s.status AS status, 
                             100 AS progresspercentage, 
                             s.created_at AS starttime, 
                             CASE WHEN s.status = 'completed' THEN s.completed_at ELSE NULL END AS completedtime, 
                             '遞迴分析' AS currentstep,
                             '遞迴分析完成' AS statusmessage,
                             'recursive' AS analysistype
                      FROM analysis_sessions s
                      LEFT JOIN person_profile p ON s.root_person_id = p.id
                      WHERE s.status IN ('processing', 'completed', 'failed')");

                // 合併兩種分析記錄
                var allJobs = new List<dynamic>();
                allJobs.AddRange(traditionalJobs);
                allJobs.AddRange(recursiveJobs);

                // 按開始時間排序並去重（如果同一個人有多個記錄，保留最新的）
                var groupedJobs = allJobs
                    .GroupBy(job => job.personid)
                    .Select(group => group.OrderByDescending(job => job.starttime).First())
                    .OrderByDescending(job => job.starttime)
                    .Take(20)
                    .ToList();

                var result = new List<object>();
                foreach (dynamic job in groupedJobs)
                {
                    result.Add(new
                    {
                        PersonId = job.personid,
                        PersonName = job.personname ?? $"人員 {job.personid}",
                        Status = job.status,
                        ProgressPercentage = job.progresspercentage,
                        StartTime = job.starttime,
                        CompletedTime = job.completedtime,
                        CurrentStep = job.currentstep,
                        StatusMessage = job.statusmessage,
                        ErrorMessage = (string?)null
                    });
                }

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取分析工作列表失敗");
                return StatusCode(500, new { success = false, message = "獲取分析工作列表失敗" });
            }
        }

        [HttpDelete("stop/{personId}")]
        public async Task<IActionResult> StopAnalysis(int personId)
        {
            try
            {
                var result = await _analysisService.StopAnalysis(personId);
                if (result)
                {
                    return Ok(new { success = true, message = "分析工作已終止" });
                }
                else
                {
                    // 檢查工作是否存在
                    using var connection = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                    await connection.OpenAsync();
                    
                    var job = await connection.QueryFirstOrDefaultAsync(
                        "SELECT status FROM analysis_results WHERE person_id = @PersonId",
                        new { PersonId = personId });
                    
                    if (job == null)
                    {
                        return BadRequest(new { success = false, message = "找不到指定的分析工作" });
                    }
                    else
                    {
                        return BadRequest(new { success = false, message = $"分析工作狀態為 '{job.status}'，無法終止" });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "終止分析工作失敗: PersonId = {PersonId}", personId);
                return StatusCode(500, new { success = false, message = "終止分析工作失敗" });
            }
        }

        [HttpPost("reset/{personId}")]
        public async Task<IActionResult> ResetAnalysis(int personId)
        {
            try
            {
                using var connection = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                await connection.OpenAsync();
                
                // 檢查是否有進行中的任務
                var activeJob = await connection.QueryFirstOrDefaultAsync(
                    "SELECT status FROM analysis_results WHERE person_id = @PersonId AND status IN ('pending', 'processing')",
                    new { PersonId = personId });
                
                if (activeJob != null)
                {
                    return BadRequest(new { success = false, message = "該人員有進行中的分析任務，請先停止任務" });
                }
                
                // 刪除所有相關的分析記錄和會話
                var deletedResults = await connection.ExecuteAsync(
                    "DELETE FROM analysis_results WHERE person_id = @PersonId",
                    new { PersonId = personId });
                
                var deletedSessions = await connection.ExecuteAsync(
                    "DELETE FROM analysis_sessions WHERE root_person_id = @PersonId",
                    new { PersonId = personId });
                
                var deletedLayers = await connection.ExecuteAsync(
                    "DELETE FROM relationship_layers WHERE analysis_session_id IN (SELECT id FROM analysis_sessions WHERE root_person_id = @PersonId)",
                    new { PersonId = personId });
                
                var deletedCount = deletedResults + deletedSessions + deletedLayers;
                
                if (deletedCount > 0)
                {
                    _logger.LogInformation("已重置分析狀態: PersonId = {PersonId}, 刪除記錄數 = {Count}", personId, deletedCount);
                    return Ok(new { success = true, message = "分析狀態已重置，可以重新開始分析" });
                }
                else
                {
                    return Ok(new { success = true, message = "沒有找到需要重置的分析記錄" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重置分析狀態失敗: PersonId = {PersonId}", personId);
                return StatusCode(500, new { success = false, message = "重置分析狀態失敗" });
            }
        }

        [HttpGet("result/{personId}")]
        public async Task<IActionResult> GetAnalysisResult(int personId)
        {
            try
            {
                using var connection = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                await connection.OpenAsync();
                
                // 首先檢查遞迴分析會話
                var session = await connection.QueryFirstOrDefaultAsync(
                    @"SELECT s.root_person_id AS personid,
                             p.name AS personname,
                             s.status AS status,
                             s.total_relationships AS totalrelationships,
                             s.created_at AS createdat,
                             s.completed_at AS completedat
                      FROM analysis_sessions s
                      LEFT JOIN person_profile p ON s.root_person_id = p.id
                      WHERE s.root_person_id = @PersonId AND s.status = 'completed'
                      ORDER BY s.created_at DESC
                      LIMIT 1",
                    new { PersonId = personId });
                
                if (session != null)
                {
                    // 獲取遞迴分析的關係結果
                    var relationships = await connection.QueryAsync(
                        @"SELECT rl.source_person_id, rl.target_person_id, rl.relation_type, rl.source_field, rl.layer_depth,
                                 s.name AS source_name, t.name AS target_name
                          FROM relationship_layers rl
                          LEFT JOIN person_profile s ON rl.source_person_id = s.id
                          LEFT JOIN person_profile t ON rl.target_person_id = t.id
                          WHERE rl.analysis_session_id = (SELECT id FROM analysis_sessions WHERE root_person_id = @PersonId AND status = 'completed' ORDER BY created_at DESC LIMIT 1)
                          ORDER BY rl.layer_depth, rl.source_person_id",
                        new { PersonId = personId });
                    
                    return Ok(new { 
                        success = true, 
                        data = new {
                            PersonId = session.personid,
                            PersonName = session.personname ?? $"人員 {session.personid}",
                            AnalysisResult = relationships,
                            AnalysisDate = session.completedat,
                            Status = session.status,
                            TotalRelationships = session.totalrelationships
                        }
                    });
                }
                
                // 如果沒有遞迴分析會話，檢查傳統分析結果
                var result = await connection.QueryFirstOrDefaultAsync(
                    @"SELECT ar.person_id AS personid,
                             p.name AS personname,
                             ar.analysis_result AS analysisresult,
                             ar.analysis_date AS analysisdate,
                             ar.status AS status
                      FROM analysis_results ar
                      LEFT JOIN person_profile p ON ar.person_id = p.id
                      WHERE ar.person_id = @PersonId AND ar.status = 'completed'
                      ORDER BY ar.updated_at DESC
                      LIMIT 1",
                    new { PersonId = personId });
                
                if (result == null)
                {
                    return NotFound(new { 
                        success = false, 
                        message = "找不到該人員的分析結果" 
                    });
                }

                return Ok(new { 
                    success = true, 
                    data = new {
                        PersonId = result.personid,
                        PersonName = result.personname ?? $"人員 {result.personid}",
                        AnalysisResult = result.analysisresult,
                        AnalysisDate = result.analysisdate,
                        Status = result.status
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取分析結果失敗: PersonId = {PersonId}", personId);
                return StatusCode(500, new { 
                    success = false, 
                    message = "獲取分析結果時發生錯誤" 
                });
            }
        }

        [HttpGet("layers/{personId}")]
        public async Task<IActionResult> GetRelationshipLayers(int personId, [FromQuery] int maxDepth = 10)
        {
            try
            {
                using var connection = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                await connection.OpenAsync();

                // 獲取最新的分析會話
                var session = await connection.QueryFirstOrDefaultAsync(
                    @"SELECT id, root_person_id, max_depth, status, total_relationships, created_at, completed_at
                      FROM analysis_sessions 
                      WHERE root_person_id = @PersonId AND status = 'completed'
                      ORDER BY created_at DESC 
                      LIMIT 1",
                    new { PersonId = personId });

                if (session == null)
                {
                    return NotFound(new { 
                        success = false, 
                        message = "找不到該人員的遞迴分析結果" 
                    });
                }

                // 獲取指定深度內的關係
                var relationships = await connection.QueryAsync(
                    @"SELECT rl.source_person_id, rl.target_person_id, rl.relation_type, rl.source_field, rl.layer_depth,
                             s.name AS source_name, t.name AS target_name
                      FROM relationship_layers rl
                      LEFT JOIN person_profile s ON rl.source_person_id = s.id
                      LEFT JOIN person_profile t ON rl.target_person_id = t.id
                      WHERE rl.analysis_session_id = @SessionId AND rl.layer_depth <= @MaxDepth
                      ORDER BY rl.layer_depth, rl.source_person_id",
                    new { SessionId = session.id, MaxDepth = maxDepth });

                // 補充缺失的關係
                var supplementedRelationships = await SupplementMissingRelationships(connection, relationships.ToList(), personId, maxDepth);

                var result = new
                {
                    SessionId = session.id,
                    RootPersonId = session.root_person_id,
                    MaxDepth = session.max_depth,
                    RequestedDepth = maxDepth,
                    TotalRelationships = session.total_relationships,
                    CreatedAt = session.created_at,
                    CompletedAt = session.completed_at,
                    Relationships = supplementedRelationships
                };

                return Ok(new { 
                    success = true, 
                    data = result 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取層級關係失敗: PersonId = {PersonId}, MaxDepth = {MaxDepth}", personId, maxDepth);
                return StatusCode(500, new { 
                    success = false, 
                    message = "獲取層級關係時發生錯誤" 
                });
            }
        }

        private async Task<List<dynamic>> SupplementMissingRelationships(NpgsqlConnection connection, List<dynamic> existingRelationships, int rootPersonId, int maxDepth)
        {
            var supplementedRelationships = new List<dynamic>(existingRelationships);
            var processedPersons = new HashSet<int>();
            
            // 收集所有在現有關係中出現的人員ID
            foreach (var rel in existingRelationships)
            {
                processedPersons.Add((int)rel.source_person_id);
                processedPersons.Add((int)rel.target_person_id);
            }

            // 對於每個找到的人員，檢查是否有缺失的關係
            foreach (var personId in processedPersons.ToList())
            {
                // 獲取該人員的完整關係資料
                var personRelations = await connection.QueryAsync(
                    @"SELECT id, name, family_relationships, friends
                      FROM person_profile 
                      WHERE id = @PersonId",
                    new { PersonId = personId });

                if (personRelations.Any())
                {
                    var person = personRelations.First();
                    
                    // 解析家庭關係
                    if (!string.IsNullOrEmpty(person.family_relationships))
                    {
                        var familyRelations = await ParseRelationships(connection, person.family_relationships, "family");
                        foreach (var relation in familyRelations)
                        {
                            // 檢查這個關係是否已經存在
                            var exists = supplementedRelationships.Any(r => 
                                r.source_person_id == personId && 
                                r.target_person_id == relation.target_person_id &&
                                r.relation_type == relation.relation_type);

                            if (!exists)
                            {
                                // 獲取目標人員的姓名
                                var targetPerson = await connection.QueryFirstOrDefaultAsync(
                                    "SELECT name FROM person_profile WHERE id = @PersonId",
                                    new { PersonId = relation.target_person_id });

                                if (targetPerson != null)
                                {
                                    // 計算層級深度
                                    var sourceDepth = existingRelationships
                                        .Where(r => r.target_person_id == personId)
                                        .Select(r => (int)r.layer_depth)
                                        .DefaultIfEmpty(0)
                                        .Max();

                                    var newDepth = sourceDepth + 1;

                                    if (newDepth <= maxDepth)
                                    {
                                        supplementedRelationships.Add(new
                                        {
                                            source_person_id = personId,
                                            target_person_id = relation.target_person_id,
                                            relation_type = relation.relation_type,
                                            source_field = "family_relationships",
                                            layer_depth = newDepth,
                                            source_name = person.name,
                                            target_name = targetPerson.name
                                        });
                                    }
                                }
                            }
                        }
                    }

                    // 解析朋友關係
                    if (!string.IsNullOrEmpty(person.friends))
                    {
                        var friendRelations = await ParseRelationships(connection, person.friends, "friend");
                        foreach (var relation in friendRelations)
                        {
                            // 檢查這個關係是否已經存在
                            var exists = supplementedRelationships.Any(r => 
                                r.source_person_id == personId && 
                                r.target_person_id == relation.target_person_id &&
                                r.relation_type == relation.relation_type);

                            if (!exists)
                            {
                                // 獲取目標人員的姓名
                                var targetPerson = await connection.QueryFirstOrDefaultAsync(
                                    "SELECT name FROM person_profile WHERE id = @PersonId",
                                    new { PersonId = relation.target_person_id });

                                if (targetPerson != null)
                                {
                                    // 計算層級深度
                                    var sourceDepth = existingRelationships
                                        .Where(r => r.target_person_id == personId)
                                        .Select(r => (int)r.layer_depth)
                                        .DefaultIfEmpty(0)
                                        .Max();

                                    var newDepth = sourceDepth + 1;

                                    if (newDepth <= maxDepth)
                                    {
                                        supplementedRelationships.Add(new
                                        {
                                            source_person_id = personId,
                                            target_person_id = relation.target_person_id,
                                            relation_type = relation.relation_type,
                                            source_field = "friends",
                                            layer_depth = newDepth,
                                            source_name = person.name,
                                            target_name = targetPerson.name
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return supplementedRelationships;
        }

        private async Task<List<dynamic>> ParseRelationships(NpgsqlConnection connection, string relationshipsText, string type)
        {
            var relations = new List<dynamic>();
            
            if (string.IsNullOrEmpty(relationshipsText))
                return relations;

            // 分割多行關係
            var lines = relationshipsText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine))
                    continue;

                // 解析家庭關係格式：關係：姓名
                if (type == "family" && trimmedLine.Contains('：'))
                {
                    var parts = trimmedLine.Split('：', 2);
                    if (parts.Length == 2)
                    {
                        var relationType = parts[0].Trim();
                        var personName = parts[1].Trim();
                        
                        // 查找人員ID
                        var targetPerson = await connection.QueryFirstOrDefaultAsync(
                            "SELECT id FROM person_profile WHERE name = @Name",
                            new { Name = personName });
                        
                        if (targetPerson != null)
                        {
                            relations.Add(new
                            {
                                relation_type = relationType,
                                target_person_id = targetPerson.id
                            });
                        }
                    }
                }
                // 解析朋友關係格式：姓名，關係
                else if (type == "friend" && trimmedLine.Contains('，'))
                {
                    var parts = trimmedLine.Split('，', 2);
                    if (parts.Length == 2)
                    {
                        var personName = parts[0].Trim();
                        var relationType = parts[1].Trim();
                        
                        // 查找人員ID
                        var targetPerson = await connection.QueryFirstOrDefaultAsync(
                            "SELECT id FROM person_profile WHERE name = @Name",
                            new { Name = personName });
                        
                        if (targetPerson != null)
                        {
                            relations.Add(new
                            {
                                relation_type = relationType,
                                target_person_id = targetPerson.id
                            });
                        }
                    }
                }
            }
            
            return relations;
        }
    }
} 