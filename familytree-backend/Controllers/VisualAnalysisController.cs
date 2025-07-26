// 視覺化分析控制器：管理視覺化分析圖的 CRUD 操作
// 主要功能：分析圖列表、新增、編輯、刪除、統計

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
    public class VisualAnalysisController : BaseController
    {
        private readonly string _connectionString;

        public VisualAnalysisController(
            ILogger<VisualAnalysisController> logger,
            IConfigurationService configurationService) 
            : base(logger, configurationService)
        {
            _connectionString = configurationService.GetConnectionString();
        }

        /// <summary>
        /// 獲取所有視覺化分析圖列表
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetVisualAnalysisGraphs([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                Logger.LogInformation($"開始獲取視覺化分析圖列表 - 頁碼: {pageNumber}, 每頁: {pageSize}");
                
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // 獲取總數
                var totalCountSql = "SELECT COUNT(*) FROM visual_analysis_graphs";
                var totalCount = await connection.QuerySingleAsync<int>(totalCountSql);

                // 計算分頁
                var offset = (pageNumber - 1) * pageSize;
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                // 獲取分頁資料
                var sql = @"
                    SELECT id, name, project_ids, updated_by as UpdatedBy, updated_at as UpdatedAt 
                    FROM visual_analysis_graphs 
                    ORDER BY updated_at DESC 
                    LIMIT @pageSize OFFSET @offset";

                var dbResults = await connection.QueryAsync(sql, new { pageSize, offset });
                
                // 手動映射並處理 project_ids 轉換
                var graphs = new List<VisualAnalysisGraphModel>();
                foreach (var row in dbResults)
                {
                    var graph = new VisualAnalysisGraphModel
                    {
                        Id = (int)row.id,
                        Name = row.name?.ToString() ?? string.Empty,
                        ProjectIds = !string.IsNullOrEmpty(row.project_ids?.ToString()) 
                            ? ParseProjectIds(row.project_ids.ToString())
                            : new List<string>(),
                        UpdatedBy = row.updated_by?.ToString() ?? string.Empty,
                        UpdatedAt = row.updated_at != null ? (DateTime)row.updated_at : DateTime.Now
                    };
                    graphs.Add(graph);
                }

                // 為每個分析圖計算關聯資訊
                foreach (var graph in graphs)
                {
                    await EnrichGraphData(connection, graph);
                }

                var response = new VisualAnalysisGraphListResponse
                {
                    Success = true,
                    Message = "獲取視覺化分析圖列表成功",
                    Graphs = graphs.ToList(),
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = totalPages
                };

                Logger.LogInformation($"成功獲取 {graphs.Count()} 個視覺化分析圖");
                return Ok(response);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "獲取視覺化分析圖列表時發生錯誤");
                return StatusCode(500, new VisualAnalysisApiResponse
                {
                    Success = false,
                    Message = "獲取視覺化分析圖列表失敗"
                });
            }
        }

        /// <summary>
        /// 創建新的視覺化分析圖
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateVisualAnalysisGraph([FromBody] CreateVisualAnalysisGraphRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new VisualAnalysisApiResponse
                    {
                        Success = false,
                        Message = "輸入資料驗證失敗",
                        Data = ModelState
                    });
                }

                Logger.LogInformation($"開始創建視覺化分析圖: {request.Name}");

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // 檢查名稱是否已存在
                var existingSql = "SELECT COUNT(*) FROM visual_analysis_graphs WHERE name = @name";
                var existingCount = await connection.QuerySingleAsync<int>(existingSql, new { name = request.Name });

                if (existingCount > 0)
                {
                    return BadRequest(new VisualAnalysisApiResponse
                    {
                        Success = false,
                        Message = "分析圖名稱已存在"
                    });
                }

                // 創建新分析圖
                var insertSql = @"
                    INSERT INTO visual_analysis_graphs (name, project_ids, updated_by, updated_at) 
                    VALUES (@name, @projectIds, @updatedBy, @updatedAt)
                    RETURNING id";

                var projectIds = string.Join(",", request.ProjectIds);
                var newId = await connection.QuerySingleAsync<int>(insertSql, new
                {
                    name = request.Name,
                    projectIds = projectIds,
                    updatedBy = request.UpdatedBy,
                    updatedAt = DateTime.Now
                });

                Logger.LogInformation($"成功創建視覺化分析圖，ID: {newId}");

                return Ok(new VisualAnalysisApiResponse
                {
                    Success = true,
                    Message = "創建視覺化分析圖成功",
                    Data = new { Id = newId }
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"創建視覺化分析圖時發生錯誤: {request.Name}");
                return StatusCode(500, new VisualAnalysisApiResponse
                {
                    Success = false,
                    Message = "創建視覺化分析圖失敗"
                });
            }
        }

        /// <summary>
        /// 更新視覺化分析圖
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateVisualAnalysisGraph(int id, [FromBody] UpdateVisualAnalysisGraphRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new VisualAnalysisApiResponse
                    {
                        Success = false,
                        Message = "輸入資料驗證失敗",
                        Data = ModelState
                    });
                }

                Logger.LogInformation($"開始更新視覺化分析圖 ID: {id}");

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // 檢查分析圖是否存在
                var existingSql = "SELECT COUNT(*) FROM visual_analysis_graphs WHERE id = @id";
                var exists = await connection.QuerySingleAsync<int>(existingSql, new { id }) > 0;

                if (!exists)
                {
                    return NotFound(new VisualAnalysisApiResponse
                    {
                        Success = false,
                        Message = "視覺化分析圖不存在"
                    });
                }

                // 更新分析圖
                var updateSql = @"
                    UPDATE visual_analysis_graphs 
                    SET name = @name, project_ids = @projectIds, updated_by = @updatedBy, updated_at = @updatedAt 
                    WHERE id = @id";

                var projectIds = string.Join(",", request.ProjectIds);
                await connection.ExecuteAsync(updateSql, new
                {
                    id = id,
                    name = request.Name,
                    projectIds = projectIds,
                    updatedBy = request.UpdatedBy,
                    updatedAt = DateTime.Now
                });

                Logger.LogInformation($"成功更新視覺化分析圖 ID: {id}");

                return Ok(new VisualAnalysisApiResponse
                {
                    Success = true,
                    Message = "更新視覺化分析圖成功"
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"更新視覺化分析圖時發生錯誤 ID: {id}");
                return StatusCode(500, new VisualAnalysisApiResponse
                {
                    Success = false,
                    Message = "更新視覺化分析圖失敗"
                });
            }
        }

        /// <summary>
        /// 刪除視覺化分析圖
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVisualAnalysisGraph(int id)
        {
            try
            {
                Logger.LogInformation($"開始刪除視覺化分析圖 ID: {id}");

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // 檢查分析圖是否存在
                var existingSql = "SELECT COUNT(*) FROM visual_analysis_graphs WHERE id = @id";
                var exists = await connection.QuerySingleAsync<int>(existingSql, new { id }) > 0;

                if (!exists)
                {
                    return NotFound(new VisualAnalysisApiResponse
                    {
                        Success = false,
                        Message = "視覺化分析圖不存在"
                    });
                }

                // 刪除分析圖
                var deleteSql = "DELETE FROM visual_analysis_graphs WHERE id = @id";
                await connection.ExecuteAsync(deleteSql, new { id });

                Logger.LogInformation($"成功刪除視覺化分析圖 ID: {id}");

                return Ok(new VisualAnalysisApiResponse
                {
                    Success = true,
                    Message = "刪除視覺化分析圖成功"
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"刪除視覺化分析圖時發生錯誤 ID: {id}");
                return StatusCode(500, new VisualAnalysisApiResponse
                {
                    Success = false,
                    Message = "刪除視覺化分析圖失敗"
                });
            }
        }

        /// <summary>
        /// 解析 project_ids 字串為列表
        /// </summary>
        private List<string> ParseProjectIds(string projectIdsString)
        {
            if (string.IsNullOrEmpty(projectIdsString))
                return new List<string>();
                
            return projectIdsString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToList();
        }

        /// <summary>
        /// 豐富分析圖資料（計算關聯人數和案件名稱）
        /// </summary>
        private async Task EnrichGraphData(NpgsqlConnection connection, VisualAnalysisGraphModel graph)
        {
            try
            {
                Logger.LogInformation($"開始豐富分析圖資料 ID: {graph.Id}, ProjectIds: {string.Join(",", graph.ProjectIds)}");
                
                if (!graph.ProjectIds.Any())
                {
                    Logger.LogInformation($"分析圖 {graph.Id} 沒有關聯專案");
                    graph.RelationCount = 0;
                    graph.Cases = new List<string>();
                    return;
                }

                var projectIds = graph.ProjectIdsList;
                Logger.LogInformation($"解析的專案ID列表: {string.Join(", ", projectIds)}");
                
                var placeholders = string.Join(",", projectIds.Select((_, i) => $"@projectId{i}"));
                var parameters = new DynamicParameters();
                
                for (int i = 0; i < projectIds.Count; i++)
                {
                    parameters.Add($"projectId{i}", projectIds[i]);
                    Logger.LogInformation($"參數 projectId{i}: {projectIds[i]}");
                }

                // 獲取專案名稱
                var projectNamesSql = $@"
                    SELECT project_name
                    FROM projects 
                    WHERE id IN ({placeholders}) AND status != 'deleted'";

                Logger.LogInformation($"執行SQL: {projectNamesSql}");
                var projectNames = await connection.QueryAsync<string>(projectNamesSql, parameters);
                graph.Cases = projectNames.ToList();
                Logger.LogInformation($"查詢到的專案名稱: {string.Join(", ", graph.Cases)}");

                // 計算關聯人數（根據專案中的人員數量）
                var relationCountSql = $@"
                    SELECT COALESCE(SUM(member_count), 0) as total_count
                    FROM (
                        SELECT COUNT(*) as member_count
                        FROM person_profile 
                        WHERE project_id IN ({placeholders})
                        GROUP BY project_id
                    ) as subquery";

                graph.RelationCount = await connection.QuerySingleOrDefaultAsync<int>(relationCountSql, parameters);
                Logger.LogInformation($"計算的關聯人數: {graph.RelationCount}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"豐富分析圖資料時發生錯誤 ID: {graph.Id}");
                graph.RelationCount = 0;
                graph.Cases = new List<string>();
            }
        }

        /// <summary>
        /// 獲取編輯器資料
        /// </summary>
        [HttpGet("{id}/editor")]
        public async Task<IActionResult> GetEditorData(int id)
        {
            try
            {
                Logger.LogInformation($"開始獲取編輯器資料 ID: {id}");

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // 獲取圖表基本資料
                var graphSql = "SELECT id, name, project_ids, updated_by, updated_at FROM visual_analysis_graphs WHERE id = @id";
                var graphRow = await connection.QuerySingleOrDefaultAsync(graphSql, new { id });
                
                if (graphRow == null)
                {
                    return NotFound(new VisualAnalysisEditorResponse
                    {
                        Success = false,
                        Message = "找不到指定的視覺化分析圖"
                    });
                }
                
                                var graph = new VisualAnalysisGraphModel
                {
                    Id = (int)graphRow.id,
                    Name = graphRow.name?.ToString() ?? string.Empty,
                    ProjectIds = !string.IsNullOrEmpty(graphRow.project_ids?.ToString()) 
                        ? ParseProjectIds(graphRow.project_ids.ToString())
                        : new List<string>(),
                    UpdatedBy = graphRow.updated_by?.ToString() ?? string.Empty,
                    UpdatedAt = graphRow.updated_at != null ? (DateTime)graphRow.updated_at : DateTime.Now
                };

                            // 檢查是否已有節點資料
            var nodesSql = @"
                SELECT 
                    van.id, van.graph_id as GraphId, van.project_id as ProjectId, 
                    van.person_id as PersonId, van.is_visible as IsVisible,
                    van.node_x as NodeX, van.node_y as NodeY,
                    van.created_at as CreatedAt, van.updated_at as UpdatedAt,
                    COALESCE(p.name, 'Unknown') as PersonName, 
                    COALESCE(p.gender, '男') as PersonGender,
                    COALESCE(p.photo_index, '') as PersonPhoto,
                    COALESCE(proj.project_name, 'Unknown') as ProjectName
                FROM visual_analysis_nodes van
                LEFT JOIN person_profile p ON van.person_id = p.id
                LEFT JOIN projects proj ON van.project_id = proj.id
                WHERE van.graph_id = @id";

                var existingNodes = await connection.QueryAsync<VisualAnalysisNodeModel>(nodesSql, new { id });

                // 調試：檢查PersonPhoto是否有正確載入
                foreach (var node in existingNodes)
                {
                    Logger.LogInformation($"節點 {node.PersonName} (ID: {node.PersonId}): PersonPhoto = '{node.PersonPhoto}'");
                }

                // 如果沒有節點資料，就建立
                if (!existingNodes.Any())
                {
                    await CreateNodesForGraph(connection, graph);
                    // 重新查詢建立的節點資料
                    existingNodes = await connection.QueryAsync<VisualAnalysisNodeModel>(nodesSql, new { id });
                }

                // 獲取關係資料
                var relationshipsSql = @"
                    SELECT 
                        rl.id,
                        rl.source_person_id as SourcePersonId,
                        rl.target_person_id as TargetPersonId,
                        rl.relation_type as RelationType,
                        rl.visual_analysis_graph_id as VisualAnalysisGraphId,
                        p1.name as SourcePersonName,
                        p2.name as TargetPersonName
                    FROM relationship_layers rl
                    LEFT JOIN person_profile p1 ON rl.source_person_id = p1.id
                    LEFT JOIN person_profile p2 ON rl.target_person_id = p2.id
                    WHERE rl.visual_analysis_graph_id = @id";

                var relationships = await connection.QueryAsync<VisualAnalysisRelationshipModel>(relationshipsSql, new { id });

                // 建立專案群組資料
                var projectGroups = await BuildProjectGroups(connection, graph, existingNodes.ToList());

                var response = new VisualAnalysisEditorResponse
                {
                    Success = true,
                    Message = "成功獲取編輯器資料",
                    Graph = graph,
                    Nodes = existingNodes.ToList(),
                    Relationships = relationships.ToList(),
                    ProjectGroups = projectGroups
                };

                Logger.LogInformation($"成功獲取編輯器資料 ID: {id}，節點數量: {existingNodes.Count()}，關係數量: {relationships.Count()}");
                return Ok(response);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"獲取編輯器資料時發生錯誤 ID: {id}");
                return StatusCode(500, new VisualAnalysisEditorResponse 
                { 
                    Success = false, 
                    Message = "獲取編輯器資料時發生錯誤" 
                });
            }
        }

        /// <summary>
        /// 更新節點可見性
        /// </summary>
        [HttpPut("{id}/nodes/visibility")]
        public async Task<IActionResult> UpdateNodeVisibility(int id, [FromBody] UpdateNodeVisibilityRequest request)
        {
            try
            {
                Logger.LogInformation($"開始更新節點可見性 ID: {id}，更新數量: {request.Updates.Count}");

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                foreach (var update in request.Updates)
                {
                    var sql = @"
                        UPDATE visual_analysis_nodes 
                        SET is_visible = @isVisible, updated_at = CURRENT_TIMESTAMP 
                        WHERE graph_id = @graphId AND project_id = @projectId AND person_id = @personId";

                    await connection.ExecuteAsync(sql, new 
                    { 
                        isVisible = update.IsVisible,
                        graphId = id,
                        projectId = update.ProjectId,
                        personId = update.PersonId
                    });
                }

                Logger.LogInformation($"成功更新節點可見性 ID: {id}");
                return Ok(new VisualAnalysisApiResponse 
                { 
                    Success = true, 
                    Message = "更新節點可見性成功" 
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"更新節點可見性時發生錯誤 ID: {id}");
                return StatusCode(500, new VisualAnalysisApiResponse 
                { 
                    Success = false, 
                    Message = "更新節點可見性時發生錯誤" 
                });
            }
        }

        /// <summary>
        /// 重置图谱节点数据 - 清除旧节点并重新生成
        /// </summary>
        [HttpPost("{id}/reset-nodes")]
        public async Task<IActionResult> ResetGraphNodes(int id)
        {
            try
            {
                Logger.LogInformation($"开始重置图谱节点数据 ID: {id}");

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // 获取图表信息
                var graphSql = "SELECT id, name, project_ids FROM visual_analysis_graphs WHERE id = @id";
                var graphRow = await connection.QuerySingleOrDefaultAsync(graphSql, new { id });
                
                if (graphRow == null)
                {
                    return NotFound(new VisualAnalysisApiResponse
                    {
                        Success = false,
                        Message = "找不到指定的视觉化分析图"
                    });
                }

                var graph = new VisualAnalysisGraphModel
                {
                    Id = (int)graphRow.id,
                    Name = graphRow.name?.ToString() ?? string.Empty,
                    ProjectIds = !string.IsNullOrEmpty(graphRow.project_ids?.ToString()) 
                        ? ParseProjectIds(graphRow.project_ids.ToString())
                        : new List<string>()
                };

                // 删除所有旧节点
                var deleteSql = "DELETE FROM visual_analysis_nodes WHERE graph_id = @id";
                await connection.ExecuteAsync(deleteSql, new { id });
                Logger.LogInformation($"已清除图谱 {id} 的所有旧节点");

                // 重新创建节点
                await CreateNodesForGraph(connection, graph);
                Logger.LogInformation($"已为图谱 {id} 重新创建节点");

                return Ok(new VisualAnalysisApiResponse
                {
                    Success = true,
                    Message = "重置图谱节点数据成功"
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"重置图谱节点数据时发生错误 ID: {id}");
                return StatusCode(500, new VisualAnalysisApiResponse
                {
                    Success = false,
                    Message = "重置图谱节点数据失败"
                });
            }
        }

        /// <summary>
        /// 為圖表建立節點資料
        /// </summary>
        private async Task CreateNodesForGraph(NpgsqlConnection connection, VisualAnalysisGraphModel graph)
        {
            try
            {
                Logger.LogInformation($"開始為圖表建立節點資料 ID: {graph.Id}");

                var projectIds = graph.ProjectIdsList;
                var placeholders = string.Join(",", projectIds.Select((_, i) => $"@projectId{i}"));
                var parameters = new DynamicParameters();
                
                for (int i = 0; i < projectIds.Count; i++)
                {
                    parameters.Add($"projectId{i}", projectIds[i]);
                }

                // 查詢所有專案中的人員
                var personsSql = $@"
                    SELECT p.id, p.project_id, p.name
                    FROM person_profile p
                    WHERE p.project_id IN ({placeholders})
                    ORDER BY p.project_id, p.name";

                var persons = await connection.QueryAsync(personsSql, parameters);

                // 批量插入節點資料
                foreach (var person in persons)
                {
                    var insertSql = @"
                        INSERT INTO visual_analysis_nodes 
                        (graph_id, project_id, person_id, is_visible, node_x, node_y, created_at, updated_at)
                        VALUES (@graphId, @projectId, @personId, @isVisible, @nodeX, @nodeY, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
                        ON CONFLICT (graph_id, project_id, person_id) DO NOTHING";

                    await connection.ExecuteAsync(insertSql, new
                    {
                        graphId = graph.Id,
                        projectId = person.project_id,
                        personId = person.id,
                        isVisible = true,
                        nodeX = 0,
                        nodeY = 0
                    });
                }

                Logger.LogInformation($"成功為圖表建立節點資料 ID: {graph.Id}，人員數量: {persons.Count()}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"為圖表建立節點資料時發生錯誤 ID: {graph.Id}");
                throw;
            }
        }

        /// <summary>
        /// 建立專案群組資料
        /// </summary>
        private async Task<List<ProjectNodeGroup>> BuildProjectGroups(NpgsqlConnection connection, VisualAnalysisGraphModel graph, List<VisualAnalysisNodeModel> nodes)
        {
            try
            {
                var projectGroups = new List<ProjectNodeGroup>();
                var projectIds = graph.ProjectIdsList;

                // 獲取專案資訊
                var placeholders = string.Join(",", projectIds.Select((_, i) => $"@projectId{i}"));
                var parameters = new DynamicParameters();
                
                for (int i = 0; i < projectIds.Count; i++)
                {
                    parameters.Add($"projectId{i}", projectIds[i]);
                }

                var projectsSql = $@"
                    SELECT id, project_name
                    FROM projects 
                    WHERE id IN ({placeholders}) AND status != 'deleted'";

                var projects = await connection.QueryAsync(projectsSql, parameters);

                foreach (var project in projects)
                {
                    var projectNodes = nodes
                        .Where(n => n.ProjectId == project.id)
                        .Select(n => new PersonNode
                        {
                            PersonId = n.PersonId,
                            Name = n.PersonName,
                            IsVisible = n.IsVisible
                        })
                        .OrderBy(p => p.Name)
                        .ToList();

                    projectGroups.Add(new ProjectNodeGroup
                    {
                        ProjectId = project.id,
                        ProjectName = project.project_name,
                        Persons = projectNodes
                    });
                }

                return projectGroups.OrderBy(pg => pg.ProjectName).ToList();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"建立專案群組資料時發生錯誤 ID: {graph.Id}");
                return new List<ProjectNodeGroup>();
            }
        }
    }
} 