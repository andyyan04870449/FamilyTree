using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Dapper;
using System.ComponentModel.DataAnnotations.Schema;

namespace familytree_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MissingPersonController : ControllerBase
    {
        private readonly string _connectionString;

        public MissingPersonController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        /// <summary>
        /// 獲取所有找不到的人員記錄
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMissingPersons([FromQuery] string? status = null, [FromQuery] int? sourcePersonId = null)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var sql = @"
                    SELECT 
                        mp.id,
                        mp.name,
                        mp.relation_type as RelationType,
                        mp.source_person_id as SourcePersonId,
                        mp.source_field as SourceField,
                        mp.analysis_session_id as AnalysisSessionId,
                        mp.layer_depth as LayerDepth,
                        mp.discovered_at as DiscoveredAt,
                        mp.status,
                        mp.resolved_person_id as ResolvedPersonId,
                        mp.notes,
                        sp.name as SourcePersonName,
                        rp.name as ResolvedPersonName
                    FROM missing_persons mp
                    LEFT JOIN person_profile sp ON mp.source_person_id = sp.id
                    LEFT JOIN person_profile rp ON mp.resolved_person_id = rp.id
                    WHERE 1=1";
                
                var parameters = new DynamicParameters();
                
                if (!string.IsNullOrEmpty(status))
                {
                    sql += " AND mp.status = @Status";
                    parameters.Add("@Status", status);
                }
                
                if (sourcePersonId.HasValue)
                {
                    sql += " AND mp.source_person_id = @SourcePersonId";
                    parameters.Add("@SourcePersonId", sourcePersonId.Value);
                }
                
                sql += " ORDER BY mp.discovered_at DESC";
                
                var missingPersons = await connection.QueryAsync<MissingPerson>(sql, parameters);
                
                // 添加調試信息
                foreach (var person in missingPersons)
                {
                    Console.WriteLine($"調試 - ID: {person.Id}, Name: {person.Name}, SourcePersonId: {person.SourcePersonId}, SourceField: {person.SourceField}, LayerDepth: {person.LayerDepth}");
                }
                
                return Ok(missingPersons);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 獲取特定找不到的人員記錄
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetMissingPerson(int id)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var sql = @"
                    SELECT 
                        mp.id,
                        mp.name,
                        mp.relation_type as RelationType,
                        mp.source_person_id as SourcePersonId,
                        mp.source_field as SourceField,
                        mp.analysis_session_id as AnalysisSessionId,
                        mp.layer_depth as LayerDepth,
                        mp.discovered_at as DiscoveredAt,
                        mp.status,
                        mp.resolved_person_id as ResolvedPersonId,
                        mp.notes,
                        sp.name as SourcePersonName,
                        rp.name as ResolvedPersonName
                    FROM missing_persons mp
                    LEFT JOIN person_profile sp ON mp.source_person_id = sp.id
                    LEFT JOIN person_profile rp ON mp.resolved_person_id = rp.id
                    WHERE mp.id = @Id";
                
                var missingPerson = await connection.QueryFirstOrDefaultAsync<MissingPerson>(sql, new { Id = id });
                
                if (missingPerson == null)
                    return NotFound();
                
                return Ok(missingPerson);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 更新找不到的人員記錄狀態
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMissingPerson(int id, [FromBody] UpdateMissingPersonRequest request)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var sql = @"
                    UPDATE missing_persons 
                    SET status = @Status, 
                        resolved_person_id = @ResolvedPersonId,
                        notes = @Notes
                    WHERE id = @Id";
                
                var parameters = new
                {
                    Id = id,
                    request.Status,
                    request.ResolvedPersonId,
                    request.Notes
                };
                
                var rowsAffected = await connection.ExecuteAsync(sql, parameters);
                
                if (rowsAffected == 0)
                    return NotFound();
                
                return Ok(new { message = "更新成功" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 刪除找不到的人員記錄
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMissingPerson(int id)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var rowsAffected = await connection.ExecuteAsync(
                    "DELETE FROM missing_persons WHERE id = @Id",
                    new { Id = id });
                
                if (rowsAffected == 0)
                    return NotFound();
                
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 獲取統計信息
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var sql = @"
                    SELECT 
                        status,
                        COUNT(*) as count
                    FROM missing_persons 
                    GROUP BY status";
                
                var stats = await connection.QueryAsync(sql);
                
                var totalSql = "SELECT COUNT(*) FROM missing_persons";
                var total = await connection.ExecuteScalarAsync<int>(totalSql);
                
                return Ok(new { stats, total });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    public class MissingPerson
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        [Column("relation_type")]
        public string RelationType { get; set; } = "";
        [Column("source_person_id")]
        public int SourcePersonId { get; set; }
        [Column("source_field")]
        public string SourceField { get; set; } = "";
        [Column("analysis_session_id")]
        public string AnalysisSessionId { get; set; } = "";
        [Column("layer_depth")]
        public int LayerDepth { get; set; }
        [Column("discovered_at")]
        public DateTime DiscoveredAt { get; set; }
        public string Status { get; set; } = "";
        [Column("resolved_person_id")]
        public int? ResolvedPersonId { get; set; }
        public string? Notes { get; set; }
        [Column("source_person_name")]
        public string? SourcePersonName { get; set; }
        [Column("resolved_person_name")]
        public string? ResolvedPersonName { get; set; }
    }

    public class UpdateMissingPersonRequest
    {
        public string Status { get; set; } = "";
        public int? ResolvedPersonId { get; set; }
        public string? Notes { get; set; }
    }
} 