using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace YourNamespace.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LabController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public LabController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateLab([FromBody] LabModel lab)
        {
            if (lab == null || string.IsNullOrWhiteSpace(lab.LabId) ||
                string.IsNullOrWhiteSpace(lab.LabName) ||
                string.IsNullOrWhiteSpace(lab.Department) ||
                lab.Systems <= 0)
            {
                return BadRequest(new { message = "⚠️ Invalid lab data." });
            }

            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");

                using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                var query = @"
            INSERT INTO labs (lab_id, lab_name, department, systems)
            VALUES (@labId, @labName, @department, @systems)
        ";

                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@labId", lab.LabId);
                cmd.Parameters.AddWithValue("@labName", lab.LabName);
                cmd.Parameters.AddWithValue("@department", lab.Department);
                cmd.Parameters.AddWithValue("@systems", lab.Systems);

                await cmd.ExecuteNonQueryAsync();

                return Ok(new { message = "✅ Lab created successfully." });
            }
            catch (PostgresException ex) when (ex.SqlState == "23505") // Unique violation
            {
                return Conflict(new { message = "❌ Lab ID already exists." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "❌ Internal Server Error", error = ex.Message });
            }
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateLab([FromBody] LabModel lab)
        {
            if (lab == null || string.IsNullOrWhiteSpace(lab.LabId))
            {
                return BadRequest(new { message = "⚠️ Invalid lab data." });
            }

            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");

                using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                var query = @"
    UPDATE labs
    SET lab_name = @labName,
        department = @department,
        systems = @systems
    WHERE lab_id = @labId
";

                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@labId", lab.LabId);
                cmd.Parameters.AddWithValue("@labName", lab.LabName);
                cmd.Parameters.AddWithValue("@department", lab.Department);
                cmd.Parameters.AddWithValue("@systems", lab.Systems);

                var rows = await cmd.ExecuteNonQueryAsync();

                if (rows > 0)
                    return Ok(new { message = "✅ Lab updated successfully." });
                else
                    return NotFound(new { message = "❌ Lab not found." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "❌ Update failed.", error = ex.Message });
            }
        }


        [HttpGet("timetable")]
        public async Task<IActionResult> GetLabTimetable()
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");

                using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                var query = @"SELECT id, lab_id, subject_code, subject_name, staff_assigned, department,
                             year, semester, section, day, hour
                      FROM lab_timetable";

                using var cmd = new NpgsqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                var timetable = new List<object>();

                while (await reader.ReadAsync())
                {
                    timetable.Add(new
                    {
                        Id = reader["id"],
                        LabId = reader["lab_id"],
                        SubjectCode = reader["subject_code"],
                        SubjectName = reader["subject_name"],
                        StaffAssigned = reader["staff_assigned"],
                        Department = reader["department"],
                        Year = reader["year"],
                        Semester = reader["semester"],
                        Section = reader["section"],
                        Day = reader["day"],
                        Hour = reader["hour"]
                    });
                }

                return Ok(timetable);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "❌ Failed to retrieve lab timetable.", error = ex.Message });
            }
        }




        [HttpGet("all")]
        public async Task<IActionResult> GetAllLabs()
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");

                using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                var query = "SELECT lab_id, lab_name, department, systems FROM labs";
                using var cmd = new NpgsqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                var labs = new List<object>();

                while (await reader.ReadAsync())
                {
                    labs.Add(new
                    {
                        LabId = reader["lab_id"].ToString(),
                        LabName = reader["lab_name"].ToString(),
                        Department = reader["department"].ToString(),
                        Systems = Convert.ToInt32(reader["systems"])
                    });
                }

                return Ok(labs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "❌ Failed to retrieve labs.", error = ex.Message });
            }
        }


        public class LabModel
        {
            public string LabId { get; set; }
            public string LabName { get; set; }
            public string Department { get; set; }
            public int Systems { get; set; }
        }
        // ✅ DTOs
    }
}