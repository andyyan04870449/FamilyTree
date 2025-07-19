using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Dapper;

namespace familytree_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PersonController : ControllerBase
    {
        private readonly string _connectionString;

        public PersonController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        [HttpGet]
        public async Task<IActionResult> GetPersons()
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var persons = await connection.QueryAsync<Person>(
                    "SELECT id, name, gender, birthday, nationality, mobile, phone, id_number, passport_number, family_relationships, friends, extra_data as profiledata, created_at, updated_at FROM person_profile ORDER BY created_at DESC");
                
                return Ok(persons);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPerson(int id)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var person = await connection.QueryFirstOrDefaultAsync<Person>(
                    "SELECT id, name, gender, birthday, nationality, mobile, phone, id_number, passport_number, family_relationships, friends, extra_data as profiledata, created_at, updated_at FROM person_profile WHERE id = @id",
                    new { id });
                
                if (person == null)
                    return NotFound();
                
                return Ok(person);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreatePerson([FromBody] Person person)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var sql = @"INSERT INTO person_profile (name, gender, birthday, nationality, mobile, extra_data, created_at, updated_at) 
                           VALUES (@name, @gender, @birthday, @nationality, @mobile, @profiledata, @createdat, @updatedat) 
                           RETURNING id";
                
                var parameters = new
                {
                    person.Name,
                    person.Gender,
                    Birthday = string.IsNullOrEmpty(person.Birthday) ? (DateTime?)null : DateTime.Parse(person.Birthday),
                    person.Nationality,
                    person.Mobile,
                    person.ProfileData,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                
                var newId = await connection.ExecuteScalarAsync<int>(sql, parameters);
                
                person.Id = newId;
                person.CreatedAt = DateTime.UtcNow;
                person.UpdatedAt = DateTime.UtcNow;
                
                return CreatedAtAction(nameof(GetPerson), new { id = person.Id }, person);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePerson(int id, [FromBody] Person person)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var sql = @"UPDATE person_profile 
                           SET name = @name, gender = @gender, birthday = @birthday, nationality = @nationality, 
                               mobile = @mobile, extra_data = @profiledata, updated_at = @updatedat 
                           WHERE id = @id";
                
                var parameters = new
                {
                    id,
                    person.Name,
                    person.Gender,
                    Birthday = string.IsNullOrEmpty(person.Birthday) ? (DateTime?)null : DateTime.Parse(person.Birthday),
                    person.Nationality,
                    person.Mobile,
                    person.ProfileData,
                    UpdatedAt = DateTime.UtcNow
                };
                
                var rowsAffected = await connection.ExecuteAsync(sql, parameters);
                
                if (rowsAffected == 0)
                    return NotFound();
                
                person.Id = id;
                person.UpdatedAt = DateTime.UtcNow;
                
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
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
} 