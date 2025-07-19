using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace MyApp.Namespace
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
                var persons = new List<Person>();
                
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new NpgsqlCommand(
                        "SELECT id, name, gender, birthday, nationality, mobile, phone, id_number, passport_number, family_relationships, friends, extra_data, created_at, updated_at FROM person_profile ORDER BY created_at DESC", 
                        connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                persons.Add(new Person
                                {
                                    Id = reader.GetInt32("id"),
                                    Name = reader.IsDBNull("name") ? "" : reader.GetString("name"),
                                    Gender = reader.IsDBNull("gender") ? "" : reader.GetString("gender"),
                                    Birthday = reader.IsDBNull("birthday") ? "" : reader.GetDateTime("birthday").ToString("yyyy-MM-dd"),
                                    Nationality = reader.IsDBNull("nationality") ? "" : reader.GetString("nationality"),
                                    Mobile = reader.IsDBNull("mobile") ? "" : reader.GetString("mobile"),
                                    Phone = reader.IsDBNull("phone") ? "" : reader.GetString("phone"),
                                    IdNumber = reader.IsDBNull("id_number") ? "" : reader.GetString("id_number"),
                                    PassportNumber = reader.IsDBNull("passport_number") ? "" : reader.GetString("passport_number"),
                                    FamilyRelationships = reader.IsDBNull("family_relationships") ? "" : reader.GetString("family_relationships"),
                                    Friends = reader.IsDBNull("friends") ? "" : reader.GetString("friends"),
                                    ProfileData = reader.IsDBNull("extra_data") ? null : reader.GetString("extra_data"),
                                    CreatedAt = reader.GetDateTime("created_at"),
                                    UpdatedAt = reader.GetDateTime("updated_at")
                                });
                            }
                        }
                    }
                }

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
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new NpgsqlCommand(
                        "SELECT id, name, gender, birthday, nationality, mobile, phone, id_number, passport_number, family_relationships, friends, extra_data, created_at, updated_at FROM person_profile WHERE id = @id", 
                        connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var person = new Person
                                {
                                    Id = reader.GetInt32("id"),
                                    Name = reader.IsDBNull("name") ? "" : reader.GetString("name"),
                                    Gender = reader.IsDBNull("gender") ? "" : reader.GetString("gender"),
                                    Birthday = reader.IsDBNull("birthday") ? "" : reader.GetDateTime("birthday").ToString("yyyy-MM-dd"),
                                    Nationality = reader.IsDBNull("nationality") ? "" : reader.GetString("nationality"),
                                    Mobile = reader.IsDBNull("mobile") ? "" : reader.GetString("mobile"),
                                    Phone = reader.IsDBNull("phone") ? "" : reader.GetString("phone"),
                                    IdNumber = reader.IsDBNull("id_number") ? "" : reader.GetString("id_number"),
                                    PassportNumber = reader.IsDBNull("passport_number") ? "" : reader.GetString("passport_number"),
                                    FamilyRelationships = reader.IsDBNull("family_relationships") ? "" : reader.GetString("family_relationships"),
                                    Friends = reader.IsDBNull("friends") ? "" : reader.GetString("friends"),
                                    ProfileData = reader.IsDBNull("extra_data") ? null : reader.GetString("extra_data"),
                                    CreatedAt = reader.GetDateTime("created_at"),
                                    UpdatedAt = reader.GetDateTime("updated_at")
                                };

                                return Ok(person);
                            }
                        }
                    }
                }

                return NotFound();
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
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new NpgsqlCommand(
                        @"INSERT INTO person_profile (name, gender, birthday, nationality, mobile, extra_data, created_at, updated_at) 
                          VALUES (@name, @gender, @birthday, @nationality, @mobile, @extraData, @createdAt, @updatedAt) 
                          RETURNING id", 
                        connection))
                    {
                        command.Parameters.AddWithValue("@name", person.Name);
                        command.Parameters.AddWithValue("@gender", person.Gender);
                        command.Parameters.AddWithValue("@birthday", string.IsNullOrEmpty(person.Birthday) ? DBNull.Value : (object)DateTime.Parse(person.Birthday));
                        command.Parameters.AddWithValue("@nationality", person.Nationality);
                        command.Parameters.AddWithValue("@mobile", person.Mobile);
                        command.Parameters.AddWithValue("@extraData", (object)person.ProfileData ?? DBNull.Value);
                        command.Parameters.AddWithValue("@createdAt", DateTime.UtcNow);
                        command.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow);

                        var newId = await command.ExecuteScalarAsync();
                        person.Id = Convert.ToInt32(newId);
                        person.CreatedAt = DateTime.UtcNow;
                        person.UpdatedAt = DateTime.UtcNow;

                        return CreatedAtAction(nameof(GetPerson), new { id = person.Id }, person);
                    }
                }
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
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new NpgsqlCommand(
                        @"UPDATE person_profile 
                          SET name = @name, gender = @gender, birthday = @birthday, nationality = @nationality, 
                              mobile = @mobile, extra_data = @extraData, updated_at = @updatedAt 
                          WHERE id = @id", 
                        connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        command.Parameters.AddWithValue("@name", person.Name);
                        command.Parameters.AddWithValue("@gender", person.Gender);
                        command.Parameters.AddWithValue("@birthday", string.IsNullOrEmpty(person.Birthday) ? DBNull.Value : (object)DateTime.Parse(person.Birthday));
                        command.Parameters.AddWithValue("@nationality", person.Nationality);
                        command.Parameters.AddWithValue("@mobile", person.Mobile);
                        command.Parameters.AddWithValue("@extraData", (object)person.ProfileData ?? DBNull.Value);
                        command.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow);

                        var rowsAffected = await command.ExecuteNonQueryAsync();
                        
                        if (rowsAffected == 0)
                            return NotFound();

                        person.Id = id;
                        person.UpdatedAt = DateTime.UtcNow;

                        return Ok(person);
                    }
                }
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
                var persons = new List<Person>();
                var conditions = new List<string>();
                var parameters = new List<NpgsqlParameter>();
                
                if (!string.IsNullOrEmpty(name))
                {
                    conditions.Add("name ILIKE @name");
                    parameters.Add(new NpgsqlParameter("@name", $"%{name}%"));
                }
                
                if (!string.IsNullOrEmpty(birthDate))
                {
                    // 驗證日期格式
                    if (DateTime.TryParse(birthDate, out DateTime parsedDate))
                    {
                        conditions.Add("DATE(birthday) = @birthDate::date");
                        parameters.Add(new NpgsqlParameter("@birthDate", parsedDate.ToString("yyyy-MM-dd")));
                    }
                    else
                    {
                        return BadRequest(new { error = "Invalid date format. Please use YYYY-MM-DD format." });
                    }
                }
                
                var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";
                
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new NpgsqlCommand(
                        $"SELECT id, name, gender, birthday, nationality, mobile, phone, id_number, passport_number, family_relationships, friends, extra_data, created_at, updated_at FROM person_profile {whereClause} ORDER BY created_at DESC", 
                        connection))
                    {
                        foreach (var parameter in parameters)
                        {
                            command.Parameters.Add(parameter);
                        }
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                persons.Add(new Person
                                {
                                    Id = reader.GetInt32("id"),
                                    Name = reader.IsDBNull("name") ? "" : reader.GetString("name"),
                                    Gender = reader.IsDBNull("gender") ? "" : reader.GetString("gender"),
                                    Birthday = reader.IsDBNull("birthday") ? "" : reader.GetDateTime("birthday").ToString("yyyy-MM-dd"),
                                    Nationality = reader.IsDBNull("nationality") ? "" : reader.GetString("nationality"),
                                    Mobile = reader.IsDBNull("mobile") ? "" : reader.GetString("mobile"),
                                    Phone = reader.IsDBNull("phone") ? "" : reader.GetString("phone"),
                                    IdNumber = reader.IsDBNull("id_number") ? "" : reader.GetString("id_number"),
                                    PassportNumber = reader.IsDBNull("passport_number") ? "" : reader.GetString("passport_number"),
                                    FamilyRelationships = reader.IsDBNull("family_relationships") ? "" : reader.GetString("family_relationships"),
                                    Friends = reader.IsDBNull("friends") ? "" : reader.GetString("friends"),
                                    ProfileData = reader.IsDBNull("extra_data") ? null : reader.GetString("extra_data"),
                                    CreatedAt = reader.GetDateTime("created_at"),
                                    UpdatedAt = reader.GetDateTime("updated_at")
                                });
                            }
                        }
                    }
                }

                return Ok(persons);
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
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new NpgsqlCommand(
                        "DELETE FROM person_profile WHERE id = @id", 
                        connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        
                        var rowsAffected = await command.ExecuteNonQueryAsync();
                        
                        if (rowsAffected == 0)
                            return NotFound();

                        return NoContent();
                    }
                }
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
        public string Name { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string Birthday { get; set; } = string.Empty;
        public string Nationality { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string? ProfileData { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? Phone { get; set; }
        public string? IdNumber { get; set; }
        public string? PassportNumber { get; set; }
        public string? FamilyRelationships { get; set; }
        public string? Friends { get; set; }
    }
} 