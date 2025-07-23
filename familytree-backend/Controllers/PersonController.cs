using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Dapper;
using System.IO;
using familytree_backend.Models;

namespace familytree_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PersonController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly IWebHostEnvironment _environment;

        public PersonController(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _environment = environment;
        }

        private void LogToFile(string message)
        {
            try
            {
                var logDirectory = Path.Combine(_environment.ContentRootPath, "logs");
                var logFilePath = Path.Combine(logDirectory, "familytree-analysis-.log");
                
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                var logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - PersonController - {message}";
                System.IO.File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
            }
            catch
            {
                // 日誌寫入失敗時不中斷主要業務流程
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPersons([FromQuery] string? project_id = null)
        {
            try
            {
                LogToFile($"開始獲取人員資料 - 專案ID: {project_id}");
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var sql = @"
                    SELECT 
                        id,
                        name,
                        TRIM(gender) as gender,
                        birthday,
                        nationality,
                        mobile,
                        phone,
                        id_number as IdNumber,
                        passport_number as PassportNumber,
                        family_relationships as FamilyRelationships,
                        friends as ImportantFriends,
                        extra_data as profiledata,
                        '2025-01-01' as CreatedAt,
                        '2025-01-01' as UpdatedAt,
                        photo_index as Photo,
                        discovery_source as DiscoveryProcess,
                        birthplace as Birthplace,
                        ethnicity as Ethnicity,
                        ancestral_origin as AncestralHome,
                        political_party as PoliticalParty,
                        email as Email,
                        current_employer as CurrentWorkplace,
                        address as CurrentAddress,
                        mailing_address as MailingAddress,
                        experience as Experience,
                        education as Education,
                        online_accounts as OnlineAccounts,
                        publications as Publications,
                        activities as Activities,
                        frequent_locations as FrequentPlaces,
                        travel_history as TravelRecords,
                        remarks as Notes,
                        file_md5 as FileMd5,
                        project_id
                    FROM person_profile
                    WHERE (@project_id IS NULL OR project_id = @project_id)
                    ORDER BY id DESC";
                
                var persons = await connection.QueryAsync<PersonDataModel>(sql, new { project_id });
                
                LogToFile($"成功獲取 {persons.Count()} 筆人員資料 - 專案ID: {project_id}");
                return Ok(new PersonDataListResponse
                { 
                    Success = true,
                    Message = "成功獲取人員資料",
                    PersonDataList = persons.ToList(),
                    TotalCount = persons.Count(),
                    PageNumber = 1,
                    PageSize = persons.Count(),
                    TotalPages = 1
                });
            }
            catch (Exception ex)
            {
                LogToFile($"獲取人員資料時發生錯誤: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPerson(int id, [FromQuery] string? project_id = null)
        {
            try
            {
                LogToFile($"開始獲取ID為 {id} 的人員資料 - 專案ID: {project_id}");
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var person = await connection.QueryFirstOrDefaultAsync<Person>(
                    "SELECT id, name, gender, birthday, nationality, mobile, phone, id_number, passport_number, family_relationships, friends, extra_data as profiledata, created_at, updated_at FROM person_profile WHERE id = @id AND (@project_id IS NULL OR project_id = @project_id)",
                    new { id, project_id });
                
                if (person == null)
                {
                    LogToFile($"未找到ID為 {id} 的人員資料");
                    return NotFound();
                }
                
                LogToFile($"成功獲取ID為 {id} 的人員資料");
                return Ok(person);
            }
            catch (Exception ex)
            {
                LogToFile($"獲取ID為 {id} 的人員資料時發生錯誤: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreatePerson([FromBody] PersonDataModel person)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var sql = @"INSERT INTO person_profile (name, gender, birthday, nationality, mobile, created_at, updated_at) 
                           VALUES (@Name, @Gender, @Birthday, @Nationality, @Mobile, @CreatedAt, @UpdatedAt) 
                           RETURNING id";
                
                var parameters = new
                {
                    person.Name,
                    person.Gender,
                    person.Birthday,
                    person.Nationality,
                    person.Mobile,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                
                var newId = await connection.ExecuteScalarAsync<int>(sql, parameters);
                
                person.Id = newId;
                person.CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
                person.UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
                
                return CreatedAtAction(nameof(GetPerson), new { id = person.Id }, person);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePerson(int id, [FromBody] PersonDataModel person)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var sql = @"UPDATE person_profile 
                           SET name = @Name, gender = @Gender, birthday = @Birthday, nationality = @Nationality, 
                               mobile = @Mobile, updated_at = @UpdatedAt 
                           WHERE id = @Id";
                
                var parameters = new
                {
                    Id = id,
                    person.Name,
                    person.Gender,
                    person.Birthday,
                    person.Nationality,
                    person.Mobile,
                    UpdatedAt = DateTime.UtcNow
                };
                
                var rowsAffected = await connection.ExecuteAsync(sql, parameters);
                
                if (rowsAffected == 0)
                    return NotFound();
                
                person.Id = id;
                person.UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
                
                return Ok(person);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePerson(int id)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var rowsAffected = await connection.ExecuteAsync(
                    "DELETE FROM person_profile WHERE id = @id",
                    new { id });
                
                if (rowsAffected == 0)
                    return NotFound();
                
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchPersons([FromQuery] string? name, [FromQuery] string? birthDate)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var sql = "SELECT id, name, gender, birthday, nationality, mobile, phone, id_number, passport_number, family_relationships, friends, extra_data as profiledata, created_at, updated_at FROM person_profile WHERE 1=1";
                var parameters = new DynamicParameters();
                
                if (!string.IsNullOrEmpty(name))
                {
                    sql += " AND name ILIKE @name";
                    parameters.Add("@name", $"%{name}%");
                }
                
                if (!string.IsNullOrEmpty(birthDate))
                {
                    sql += " AND DATE(birthday) = @birthDate";
                    parameters.Add("@birthDate", DateTime.Parse(birthDate).Date);
                }
                
                sql += " ORDER BY created_at DESC";
                
                var persons = await connection.QueryAsync<Person>(sql, parameters);
                
                return Ok(persons);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    public class Person
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Gender { get; set; } = "";
        public string Birthday { get; set; } = "";
        public string Nationality { get; set; } = "";
        public string Mobile { get; set; } = "";
        public string? Phone { get; set; }
        public string? IdNumber { get; set; }
        public string? PassportNumber { get; set; }
        public string? FamilyRelationships { get; set; }
        public string? Friends { get; set; }
        public string? ProfileData { get; set; }
        public string CreatedAt { get; set; } = "";
        public string UpdatedAt { get; set; } = "";
    }
} 