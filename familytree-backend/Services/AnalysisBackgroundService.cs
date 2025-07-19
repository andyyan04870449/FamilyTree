using System.Text.Json;
using familytree_backend.Models;
using Npgsql;
using Dapper;
using Microsoft.Extensions.Logging;

namespace familytree_backend.Services
{
    public class AnalysisBackgroundService : BackgroundService
    {
        private readonly ILogger<AnalysisBackgroundService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private readonly string _connectionString;
        private readonly Dictionary<int, RecursiveAnalysisTask> _activeTasks = new();

        public AnalysisBackgroundService(
            ILogger<AnalysisBackgroundService> logger,
            IConfiguration configuration,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _configuration = configuration;
            _serviceProvider = serviceProvider;
            _connectionString = _configuration.GetConnectionString("DefaultConnection") ?? 
                               "Host=localhost;Database=familytree;Username=postgres;Password=postgres";
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("分析背景服務已啟動");
            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // 暫時簡化，不處理待分析任務
                    await Task.Delay(5000, stoppingToken); // 每5秒檢查一次
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "背景工作執行錯誤");
                    await Task.Delay(10000, stoppingToken); // 錯誤時等待10秒
                }
            }
        }

        private async Task ProcessPendingAnalyses(CancellationToken stoppingToken)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(stoppingToken);

            // 查詢待處理的分析任務
            var pendingAnalyses = await connection.QueryAsync<AnalysisResult>(
                "SELECT * FROM analysis_results WHERE status = 'pending' ORDER BY created_at ASC");

            foreach (var analysis in pendingAnalyses)
            {
                if (stoppingToken.IsCancellationRequested) break;

                // 檢查是否已有相同人員的進行中任務
                if (_activeTasks.ContainsKey(analysis.PersonId))
                {
                    continue;
                }

                // 生成分析會話ID
                var sessionId = Guid.NewGuid().ToString();
                
                // 創建分析會話
                await connection.ExecuteAsync(@"
                    INSERT INTO analysis_sessions (id, root_person_id, max_depth, status, created_at)
                    VALUES (@SessionId, @PersonId, @MaxDepth, 'processing', CURRENT_TIMESTAMP)",
                    new { SessionId = sessionId, PersonId = analysis.PersonId, MaxDepth = 10 });

                // 開始處理
                var task = new RecursiveAnalysisTask(analysis.PersonId, sessionId, 3, _logger, _serviceProvider, _connectionString);
                _activeTasks[analysis.PersonId] = task;

                // 更新狀態為處理中
                await connection.ExecuteAsync(
                    "UPDATE analysis_results SET status = 'processing', updated_at = CURRENT_TIMESTAMP WHERE id = @Id",
                    new { analysis.Id });

                // 在背景執行分析
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await task.ExecuteRecursiveAnalysis();
                    }
                    finally
                    {
                        _activeTasks.Remove(analysis.PersonId);
                    }
                }, stoppingToken);
            }
        }

        public async Task<AnalysisProgressResponse?> GetAnalysisProgress(int personId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            // 首先檢查遞迴分析會話
            dynamic? sessionResult = await connection.QueryFirstOrDefaultAsync(
                @"SELECT s.root_person_id AS personid,
                         p.name AS personname,
                         s.status AS status,
                         s.total_relationships AS totalrelationships,
                         s.created_at AS createdat,
                         s.completed_at AS completedat
                  FROM analysis_sessions s
                  LEFT JOIN person_profile p ON s.root_person_id = p.id
                  WHERE s.root_person_id = @PersonId
                  ORDER BY s.created_at DESC
                  LIMIT 1",
                new { PersonId = personId });

            if (sessionResult != null)
            {
                // 計算進度百分比
                int progressPercentage = 0;
                string status = sessionResult.status?.ToString() ?? "unknown";
                
                if (status == "processing")
                {
                    progressPercentage = 50; // 進行中
                }
                else if (status == "completed")
                {
                    progressPercentage = 100; // 已完成
                }
                else if (status == "failed")
                {
                    progressPercentage = 0; // 失敗
                }

                return new AnalysisProgressResponse
                {
                    PersonId = sessionResult.personid ?? 0,
                    PersonName = sessionResult.personname?.ToString() ?? $"人員 {sessionResult.personid}",
                    ProgressPercentage = progressPercentage,
                    Status = status == "processing" ? "開始遞迴分析..." : status,
                    AnalysisResult = null
                };
            }

            // 如果沒有遞迴分析會話，檢查傳統分析結果
            dynamic? result = await connection.QueryFirstOrDefaultAsync(
                @"SELECT ar.person_id AS personid,
                         p.name AS personname,
                         ar.progress_percentage AS progresspercentage,
                         ar.status AS status,
                         ar.analysis_result AS analysisresult
                  FROM analysis_results ar
                  LEFT JOIN person_profile p ON ar.person_id = p.id
                  WHERE ar.person_id = @PersonId",
                new { PersonId = personId });

            if (result == null)
                return null;

            return new AnalysisProgressResponse
            {
                PersonId = result.personid ?? 0,
                PersonName = result.personname?.ToString() ?? $"人員 {result.personid}",
                ProgressPercentage = result.progresspercentage ?? 0,
                Status = result.status?.ToString() ?? "unknown",
                AnalysisResult = result.analysisresult != null ? JsonDocument.Parse(result.analysisresult.ToString()) : null
            };
        }

        public async Task<List<AnalysisJobResponse>> GetAllAnalysisJobs()
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var jobs = await connection.QueryAsync(
                    @"SELECT ar.person_id AS personid, 
                             p.name AS personname,
                             ar.status AS status, 
                             ar.progress_percentage AS progresspercentage, 
                             ar.created_at AS starttime, 
                             CASE WHEN ar.status = 'completed' THEN ar.updated_at ELSE NULL END AS completedtime, 
                             ar.current_step AS currentstep,
                             ar.status_message AS statusmessage
                      FROM analysis_results ar
                      LEFT JOIN person_profile p ON ar.person_id = p.id
                      WHERE ar.status IN ('processing', 'completed', 'failed')
                      ORDER BY ar.created_at DESC
                      LIMIT 10");

                // 轉型
                var result = new List<AnalysisJobResponse>();
                foreach (dynamic job in jobs)
                {
                    result.Add(new AnalysisJobResponse
                    {
                        PersonId = job.personid ?? 0,
                        PersonName = job.personname?.ToString() ?? $"人員 {job.personid}",
                        Status = job.status?.ToString() ?? "unknown",
                        ProgressPercentage = job.progresspercentage ?? 0,
                        StartTime = job.starttime ?? DateTime.Now,
                        CompletedTime = job.completedtime,
                        CurrentStep = job.currentstep?.ToString(),
                        StatusMessage = job.statusmessage?.ToString(),
                        ErrorMessage = null
                    });
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取分析工作列表失敗");
                throw;
            }
        }

        public async Task<bool> StopAnalysis(int personId)
        {
            try
            {
                // 檢查是否有進行中的分析任務
                if (_activeTasks.ContainsKey(personId))
                {
                    // 停止任務
                    _activeTasks[personId].Cancel();
                    _activeTasks.Remove(personId);
                    _logger.LogInformation("分析工作已終止: PersonId = {PersonId}", personId);
                    return true;
                }
                else
                {
                    // 檢查資料庫中是否有進行中的分析工作
                    using var connection = new NpgsqlConnection(_connectionString);
                    await connection.OpenAsync();
                    
                    var existingJob = await connection.QueryFirstOrDefaultAsync(
                        "SELECT status FROM analysis_results WHERE person_id = @PersonId AND status IN ('pending', 'processing')",
                        new { PersonId = personId });
                    
                    if (existingJob != null)
                    {
                        // 更新資料庫狀態為已終止
                        await connection.ExecuteAsync(
                            "UPDATE analysis_results SET status = 'failed', updated_at = CURRENT_TIMESTAMP WHERE person_id = @PersonId",
                            new { PersonId = personId });
                        
                        _logger.LogInformation("資料庫中的分析工作已標記為終止: PersonId = {PersonId}", personId);
                        return true;
                    }
                    else
                    {
                        // 檢查是否有任何狀態的工作
                        var anyJob = await connection.QueryFirstOrDefaultAsync(
                            "SELECT status FROM analysis_results WHERE person_id = @PersonId",
                            new { PersonId = personId });
                        
                        if (anyJob != null)
                        {
                            return false; // 工作存在但已完成或已終止
                        }
                        else
                        {
                            return false; // 工作不存在
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "終止分析工作失敗: PersonId = {PersonId}", personId);
                return false;
            }
        }

        public async Task<bool> StartAnalysis(int personId, int maxDepth = 10)
        {
            try
            {
                _logger.LogInformation("開始遞迴分析: PersonId = {PersonId}, MaxDepth = {MaxDepth}", personId, maxDepth);
                
                // 檢查是否已有進行中的分析任務
                if (_activeTasks.ContainsKey(personId))
                {
                    _logger.LogInformation($"該人員已有進行中的分析任務: PersonId = {personId}");
                    return false;
                }

                // 檢查資料庫中是否已有進行中的分析工作
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                // 檢查是否有進行中的分析會話
                var existingSession = await connection.QueryFirstOrDefaultAsync(
                    "SELECT id, status FROM analysis_sessions WHERE root_person_id = @PersonId AND status = 'processing'",
                    new { PersonId = personId });
                
                if (existingSession != null)
                {
                    _logger.LogInformation($"該人員已有進行中的分析會話: PersonId = {personId}, SessionId = {existingSession.id}");
                    return false;
                }

                // 生成分析會話ID
                var sessionId = Guid.NewGuid().ToString();
                
                // 創建分析會話
                await connection.ExecuteAsync(@"
                    INSERT INTO analysis_sessions (id, root_person_id, max_depth, status, created_at)
                    VALUES (@SessionId, @PersonId, @MaxDepth, 'processing', CURRENT_TIMESTAMP)",
                    new { SessionId = sessionId, PersonId = personId, MaxDepth = maxDepth });

                // 清理所有舊的分析記錄（包括 pending, completed, failed）
                var deletedCount = await connection.ExecuteAsync(
                    "DELETE FROM analysis_results WHERE person_id = @PersonId",
                    new { PersonId = personId });
                
                if (deletedCount > 0)
                {
                    _logger.LogInformation($"清理了 {deletedCount} 個舊的分析記錄: PersonId = {personId}");
                }

                // 創建新的分析記錄
                try
                {
                    await connection.ExecuteAsync(@"
                        INSERT INTO analysis_results (person_id, analysis_result, status, progress_percentage) 
                        VALUES (@PersonId, '{}', 'pending', 0)",
                        new { PersonId = personId });
                }
                catch (Npgsql.PostgresException ex) when (ex.Message.Contains("duplicate key"))
                {
                    _logger.LogInformation($"重複的分析記錄已存在: PersonId = {personId}");
                    return false;
                }

                // 創建遞迴分析任務
                var task = new RecursiveAnalysisTask(personId, sessionId, maxDepth, _logger, _serviceProvider, _connectionString);
                _activeTasks[personId] = task;

                // 更新狀態為處理中
                await connection.ExecuteAsync(
                    "UPDATE analysis_results SET status = 'processing', updated_at = CURRENT_TIMESTAMP WHERE person_id = @PersonId",
                    new { PersonId = personId });

                // 在背景執行分析
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await task.ExecuteRecursiveAnalysis();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "遞迴分析任務執行失敗: PersonId = {PersonId}", personId);
                        // 更新資料庫狀態為失敗
                        using var connection = new NpgsqlConnection(_connectionString);
                        await connection.OpenAsync();
                        await connection.ExecuteAsync(
                            "UPDATE analysis_results SET status = 'failed', updated_at = CURRENT_TIMESTAMP WHERE person_id = @PersonId",
                            new { PersonId = personId });
                        
                        // 更新會話狀態為失敗
                        await connection.ExecuteAsync(
                            "UPDATE analysis_sessions SET status = 'failed', completed_at = CURRENT_TIMESTAMP WHERE id = @SessionId",
                            new { SessionId = sessionId });
                    }
                    finally
                    {
                        _activeTasks.Remove(personId);
                    }
                });

                _logger.LogInformation("啟動遞迴分析: PersonId = {PersonId}, SessionId = {SessionId}", personId, sessionId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "啟動遞迴分析失敗: PersonId = {PersonId}", personId);
                return false;
            }
        }

        public async Task<AnalysisResultResponse?> GetAnalysisResult(int personId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            dynamic? result = await connection.QueryFirstOrDefaultAsync(
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
                return null;

            return new AnalysisResultResponse
            {
                PersonId = result.personid ?? 0,
                PersonName = result.personname?.ToString() ?? $"人員 {result.personid}",
                AnalysisResult = JsonDocument.Parse(result.analysisresult.ToString()),
                AnalysisDate = result.analysisdate ?? DateTime.Now,
                Status = result.status?.ToString() ?? "unknown"
            };
        }
    }

    public class PersonData
    {
        public int Id { get; set; }
        public string? family_relationships { get; set; }
        public string? friends { get; set; }
    }

    public class RecursiveAnalysisTask
    {
        private readonly int _personId;
        private readonly string _sessionId;
        private readonly int _maxDepth;
        private readonly ILogger<AnalysisBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly string _connectionString;
        private readonly HashSet<int> _analyzedPersons = new();
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        public RecursiveAnalysisTask(int personId, string sessionId, int maxDepth, ILogger<AnalysisBackgroundService> logger, IServiceProvider serviceProvider, string connectionString)
        {
            _personId = personId;
            _sessionId = sessionId;
            _maxDepth = maxDepth;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _connectionString = connectionString;
        }

        public void Cancel()
        {
            _cancellationTokenSource.Cancel();
        }

        public async Task ExecuteRecursiveAnalysis()
        {
            try
            {
                _logger.LogInformation("=== 開始執行遞迴分析任務: PersonId = {PersonId}, MaxDepth = {MaxDepth} ===", _personId, _maxDepth);
                await UpdateProgress(5, "開始遞迴分析...");

                // 開始遞迴分析
                await AnalyzePersonRecursively(_personId, 1);

                // 更新會話狀態為完成
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var totalRelationships = await connection.QueryFirstOrDefaultAsync<int>(
                    "SELECT COUNT(*) FROM relationship_layers WHERE analysis_session_id = @SessionId",
                    new { SessionId = _sessionId });

                await connection.ExecuteAsync(@"
                    UPDATE analysis_sessions 
                    SET status = 'completed', total_relationships = @TotalRelationships, completed_at = CURRENT_TIMESTAMP 
                    WHERE id = @SessionId",
                    new { SessionId = _sessionId, TotalRelationships = totalRelationships });

                await UpdateProgress(100, "遞迴分析完成");
                _logger.LogInformation("=== 遞迴分析任務完成: PersonId = {PersonId}, 總共找到 {Count} 個關係 ===", _personId, totalRelationships);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== 遞迴分析執行失敗: PersonId = {PersonId} ===", _personId);
                await UpdateStatus("failed", ex.Message);
                
                // 更新會話狀態為失敗
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                await connection.ExecuteAsync(
                    "UPDATE analysis_sessions SET status = 'failed', completed_at = CURRENT_TIMESTAMP WHERE id = @SessionId",
                    new { SessionId = _sessionId });
            }
        }

        private async Task AnalyzePersonRecursively(int personId, int currentDepth)
        {
            if (_cancellationTokenSource.Token.IsCancellationRequested)
            {
                _logger.LogInformation("分析被取消: PersonId = {PersonId}", personId);
                return;
            }

            if (currentDepth > _maxDepth)
            {
                _logger.LogInformation("達到最大深度限制: PersonId = {PersonId}, Depth = {Depth}", personId, currentDepth);
                return;
            }

            if (_analyzedPersons.Contains(personId))
            {
                _logger.LogInformation("人員已分析過: PersonId = {PersonId}", personId);
                return;
            }

            _analyzedPersons.Add(personId);
            _logger.LogInformation("開始分析人員: PersonId = {PersonId}, Depth = {Depth}", personId, currentDepth);

            // 查詢人員資料
            var personData = await GetPersonData(personId);
            if (personData == null)
            {
                _logger.LogWarning("找不到人員資料: PersonId = {PersonId}", personId);
                return;
            }

            _logger.LogInformation("獲取到人員資料: PersonId = {PersonId}, 家庭關係: '{FamilyRelationships}', 朋友: '{Friends}'", 
                personId, personData.family_relationships ?? "無", personData.friends ?? "無");

            var discoveredPersons = new List<int>();

            // 分析家庭關係
            if (!string.IsNullOrEmpty(personData.family_relationships))
            {
                _logger.LogInformation("分析家庭關係: PersonId = {PersonId}, Depth = {Depth}", personId, currentDepth);
                using var scope = _serviceProvider.CreateScope();
                var aiService = scope.ServiceProvider.GetRequiredService<AIService>();
                
                var familyRelations = await aiService.ExtractNameRelationsAsync(personData.family_relationships);
                _logger.LogInformation("家庭關係分析完成，找到 {Count} 個關係", familyRelations.Count);
                
                foreach (var relation in familyRelations)
                {
                    var targetId = await FindPersonIdByName(relation.Name);
                    if (targetId.HasValue)
                    {
                        await SaveRelationship(personId, targetId.Value, relation.Relation, "family_relationships", currentDepth);
                        discoveredPersons.Add(targetId.Value);
                    }
                }
            }

            // 分析朋友關係
            if (!string.IsNullOrEmpty(personData.friends))
            {
                _logger.LogInformation("分析朋友關係: PersonId = {PersonId}, Depth = {Depth}", personId, currentDepth);
                using var scope = _serviceProvider.CreateScope();
                var aiService = scope.ServiceProvider.GetRequiredService<AIService>();
                
                var friendRelations = await aiService.ExtractNameRelationsAsync(personData.friends);
                _logger.LogInformation("朋友關係分析完成，找到 {Count} 個關係", friendRelations.Count);
                
                foreach (var relation in friendRelations)
                {
                    var targetId = await FindPersonIdByName(relation.Name);
                    if (targetId.HasValue)
                    {
                        await SaveRelationship(personId, targetId.Value, relation.Relation, "friends", currentDepth);
                        discoveredPersons.Add(targetId.Value);
                    }
                }
            }

            // 更新進度
            var progress = Math.Min(95, 5 + (currentDepth * 90 / _maxDepth));
            await UpdateProgress(progress, $"分析第 {currentDepth} 層關係...");

            // 遞迴分析發現的人員
            foreach (var discoveredPersonId in discoveredPersons)
            {
                if (!_analyzedPersons.Contains(discoveredPersonId))
                {
                    await AnalyzePersonRecursively(discoveredPersonId, currentDepth + 1);
                }
            }
        }

        private async Task SaveRelationship(int sourceId, int targetId, string relation, string sourceField, int layerDepth)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                await connection.ExecuteAsync(@"
                    INSERT INTO relationship_layers (source_person_id, target_person_id, relation_type, source_field, layer_depth, analysis_session_id)
                    VALUES (@SourceId, @TargetId, @Relation, @SourceField, @LayerDepth, @SessionId)
                    ON CONFLICT (source_person_id, target_person_id, analysis_session_id) DO NOTHING",
                    new { SourceId = sourceId, TargetId = targetId, Relation = relation, SourceField = sourceField, LayerDepth = layerDepth, SessionId = _sessionId });

                _logger.LogInformation("保存關係: {SourceId} -> {TargetId} ({Relation}) at layer {LayerDepth}", sourceId, targetId, relation, layerDepth);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存關係失敗: {SourceId} -> {TargetId}", sourceId, targetId);
            }
        }

        private async Task<PersonData?> GetPersonData(int personId)
        {
            _logger.LogInformation("查詢人員資料: PersonId = {PersonId}", personId);
            
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var result = await connection.QueryFirstOrDefaultAsync<PersonData>(
                "SELECT id, family_relationships, friends FROM person_profile WHERE id = @PersonId",
                new { PersonId = personId });
            
            if (result != null)
            {
                _logger.LogInformation("資料庫查詢結果: ID={Id}, 家庭關係='{FamilyRelationships}', 朋友='{Friends}'", 
                    result.Id, result.family_relationships ?? "NULL", result.friends ?? "NULL");
            }
            else
            {
                _logger.LogWarning("資料庫查詢無結果: PersonId = {PersonId}", personId);
            }
            
            return result;
        }

        private async Task<PersonData?> GetPersonData()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var result = await connection.QueryFirstOrDefaultAsync<PersonData>(
                "SELECT id, family_relationships, friends FROM person_profile WHERE id = @PersonId",
                new { PersonId = _personId });
            
            return result;
        }

        private async Task<int?> FindPersonIdByName(string name)
        {
            _logger.LogInformation("查找人員ID: '{Name}'", name);
            
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var result = await connection.QueryFirstOrDefaultAsync<int?>(
                "SELECT id FROM person_profile WHERE name LIKE @Name",
                new { Name = $"%{name}%" });

            if (result.HasValue)
            {
                _logger.LogInformation("✅ 找到人員: '{Name}' -> ID: {Id}", name, result.Value);
            }
            else
            {
                _logger.LogWarning("❌ 找不到人員: '{Name}'", name);
            }

            return result;
        }

        private List<string> ExtractActivities(string friendsData)
        {
            var activities = new List<string>();
            if (string.IsNullOrEmpty(friendsData)) return activities;

            // 提取包含學院、大學、協會等關鍵字的活動
            var lines = friendsData.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.Contains("學院") || line.Contains("大學") || line.Contains("協會") || 
                    line.Contains("研究會") || line.Contains("中心") || line.Contains("會"))
                {
                    activities.Add(line.Trim());
                }
            }

            return activities;
        }

        private async Task UpdateProgress(int percentage, string status)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await connection.ExecuteAsync(@"
                UPDATE analysis_results 
                SET progress_percentage = @Percentage, 
                    status = @Status, 
                    current_step = @Status,
                    status_message = @Status,
                    updated_at = CURRENT_TIMESTAMP
                WHERE person_id = @PersonId AND status IN ('processing', 'pending')",
                new { Percentage = percentage, Status = status, PersonId = _personId });
        }

        private async Task UpdateStatus(string status, string errorMessage = "")
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await connection.ExecuteAsync(@"
                UPDATE analysis_results 
                SET status = @Status, 
                    current_step = @Status,
                    status_message = @ErrorMessage,
                    updated_at = CURRENT_TIMESTAMP
                WHERE person_id = @PersonId",
                new { Status = status, ErrorMessage = errorMessage, PersonId = _personId });
        }

        private async Task UpdateAnalysisResult(string resultJson, string status)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await connection.ExecuteAsync(@"
                UPDATE analysis_results 
                SET analysis_result = @ResultJson::jsonb, 
                    status = @Status, 
                    progress_percentage = 100,
                    updated_at = CURRENT_TIMESTAMP
                WHERE person_id = @PersonId",
                new { ResultJson = resultJson, Status = status, PersonId = _personId });
        }
    }
} 