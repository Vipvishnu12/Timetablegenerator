using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TimetableGA;

namespace YourNamespace.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CrossDepartmentAssignmentsController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public CrossDepartmentAssignmentsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("grouped")]
        public async Task<IActionResult> GetGroupedAssignments([FromQuery] string department)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                var results = new List<dynamic>();
                using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                var sql = @"
                    SELECT 
                        id,
                        from_department,
                        to_department,
                        subject_code,
                        subject_name,
                        year,
                        semester,
                        section,
                        assigned_at,
                        assigned_staff
                    FROM cross_department_assignments
                    WHERE from_department = @department
                    ORDER BY year, semester, section, from_department
                ";

                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@department", department);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    results.Add(new
                    {
                        id = Convert.ToInt32(reader["id"]),
                        department = reader["from_department"].ToString(),
                        subject_code = reader["subject_code"].ToString(),
                        subject_name = reader["subject_name"].ToString(),
                        year = reader["year"].ToString(),
                        semester = reader["semester"].ToString(),
                        section = reader["section"].ToString(),
                        assigned_at = reader["assigned_at"] == DBNull.Value ? null : reader["assigned_at"].ToString(),
                        assignedStaff = reader["assigned_staff"] == DBNull.Value ? null : reader["assigned_staff"].ToString()
                    });
                }

                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to retrieve data", error = ex.Message });
            }
        }

        [HttpGet("generateCrossDepartmentTimetable")]
        public async Task<IActionResult> GenerateCrossDepartmentTimetable(
      [FromQuery] string toDepartment,
      [FromQuery] string year,
      [FromQuery] string semester,
      [FromQuery] string section)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                var subjects = new List<TimetableEngine.Subject>();
                var globalStaffAvailability = new Dictionary<string, Dictionary<string, HashSet<int>>>();

                using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                // Step 1: Load Subject Assignments
                var query = @"
            SELECT * FROM subject_assignments
            WHERE LOWER(department) = LOWER(@toDepartment)
              AND LOWER(year) = LOWER(@year)
              AND LOWER(semester) = LOWER(@semester)
              AND LOWER(section) = LOWER(@section)
              AND staff_assigned IS NOT NULL AND TRIM(staff_assigned) <> ''
        ";

                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@toDepartment", toDepartment.Trim());
                cmd.Parameters.AddWithValue("@year", year.Trim());
                cmd.Parameters.AddWithValue("@semester", semester.Trim());
                cmd.Parameters.AddWithValue("@section", section.Trim());

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    subjects.Add(new TimetableEngine.Subject
                    {
                        SubjectCode = reader["sub_code"]?.ToString() ?? "---",
                        SubjectName = reader["subject_name"]?.ToString() ?? "---",
                        SubjectType = reader["subject_type"]?.ToString() ?? "Theory",
                        Credit = Convert.ToInt32(reader["credit"]),
                        StaffAssigned = reader["staff_assigned"]?.ToString() ?? "---"
                    });
                }
                reader.Close();

                if (subjects.Count == 0)
                {
                    return BadRequest(new
                    {
                        message = "❌ No valid subjects found. Ensure all have assigned staff.",
                        debug = new { toDepartment, year, semester, section }
                    });
                }

                // Step 2: Load Staff Availability from Existing Timetable
                var availabilityQuery = "SELECT staff_assigned, day, hour FROM cross_class_timetable";
                using var existingCmd = new NpgsqlCommand(availabilityQuery, conn);
                using var availabilityReader = await existingCmd.ExecuteReaderAsync();
                while (await availabilityReader.ReadAsync())
                {
                    var staff = availabilityReader["staff_assigned"].ToString();
                    var day = availabilityReader["day"].ToString();
                    var hour = Convert.ToInt32(availabilityReader["hour"]);

                    // Initialize dictionary for staff
                    if (!globalStaffAvailability.ContainsKey(staff))
                    {
                        globalStaffAvailability[staff] = new Dictionary<string, HashSet<int>>();
                        foreach (var d in new[] { "Mon", "Tue", "Wed", "Thu", "Fri" })
                            globalStaffAvailability[staff][d] = new HashSet<int>();
                    }

                    // Add available hour
                    if (!globalStaffAvailability[staff].ContainsKey(day))
                        globalStaffAvailability[staff][day] = new HashSet<int>();

                    globalStaffAvailability[staff][day].Add(hour);
                }
                availabilityReader.Close();

                // Step 3: Generate Timetable
                var engine = new TimetableEngine();
                var (timetable, conflicts) = engine.Generate(subjects, globalStaffAvailability);

                // Step 4: Save into cross_class_timetable and staff_timetable
                foreach (var daySlot in timetable)
                {
                    foreach (var kv in daySlot.HourlySlots)
                    {
                        string value = kv.Value;
                        if (value == "---") continue;

                        var hour = kv.Key;
                        var parts = value.Split("(", StringSplitOptions.TrimEntries);
                        string subjectCode = parts[0].Trim();
                        string staff = parts.Length > 1 ? parts[1].Replace(")", "").Trim() : "---";

                        // Save into cross_class_timetable
                        var insertCmd = new NpgsqlCommand(@"
                    INSERT INTO cross_class_timetable 
                    (from_department, to_department, year, semester, section, day, hour, subject_code, staff_assigned)
                    VALUES (@from_department, @to_department, @year, @semester, @section, @day, @hour, @subject_code, @staff_assigned);
                ", conn);

                        insertCmd.Parameters.AddWithValue("@from_department", toDepartment);
                        insertCmd.Parameters.AddWithValue("@to_department", toDepartment);
                        insertCmd.Parameters.AddWithValue("@year", year);
                        insertCmd.Parameters.AddWithValue("@semester", semester);
                        insertCmd.Parameters.AddWithValue("@section", section);
                        insertCmd.Parameters.AddWithValue("@day", daySlot.Day);
                        insertCmd.Parameters.AddWithValue("@hour", hour);
                        insertCmd.Parameters.AddWithValue("@subject_code", subjectCode);
                        insertCmd.Parameters.AddWithValue("@staff_assigned", staff);

                        await insertCmd.ExecuteNonQueryAsync();

                        // Save into staff_timetable
                        var staffInsertCmd = new NpgsqlCommand(@"
                    INSERT INTO staff_timetable
                    (staff_name, department, year, semester, section, day, hour, subject_code, subject_name)
                    VALUES
                    (@staff_name, @department, @year, @semester, @section, @day, @hour, @subject_code, @subject_name);
                ", conn);

                        staffInsertCmd.Parameters.AddWithValue("@staff_name", staff);
                        staffInsertCmd.Parameters.AddWithValue("@department", toDepartment);
                        staffInsertCmd.Parameters.AddWithValue("@year", year);
                        staffInsertCmd.Parameters.AddWithValue("@semester", semester);
                        staffInsertCmd.Parameters.AddWithValue("@section", section);
                        staffInsertCmd.Parameters.AddWithValue("@day", daySlot.Day);
                        staffInsertCmd.Parameters.AddWithValue("@hour", hour);
                        staffInsertCmd.Parameters.AddWithValue("@subject_code", subjectCode);

                        var matchedSubject = subjects.FirstOrDefault(s => s.SubjectCode == subjectCode);
                        staffInsertCmd.Parameters.AddWithValue("@subject_name", matchedSubject?.SubjectName ?? "---");

                        await staffInsertCmd.ExecuteNonQueryAsync();
                    }
                }

                // Final response
                return Ok(new
                {
                    message = conflicts.Count == 0
                        ? "✅ Timetable generated and stored successfully."
                        : "⚠️ Timetable generated with some conflicts. Stored valid entries.",
                    timetable,
                    conflicts = conflicts.Select(c => new
                    {
                        subject = c.Subject.SubjectCode,
                        staff = c.Subject.StaffAssigned,
                        reason = c.Reason
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "❌ Internal Server Error while generating timetable.",
                    error = ex.Message
                });
            }
        }




        [HttpGet("getCrossTimetable")]
        public async Task<IActionResult> GetCrossTimetable(
            [FromQuery] string toDepartment,
            [FromQuery] string year,
            [FromQuery] string semester,
            [FromQuery] string section)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                var result = new Dictionary<string, Dictionary<int, string>>();

                using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                // ✅ Do NOT use LOWER() here since actual values are case-sensitive
                var query = @"
            SELECT day, hour, subject_code, staff_assigned
            FROM cross_class_timetable
            WHERE to_department = @toDepartment
              AND year = @year
              AND semester = @semester
              AND section = @section
        ";

                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@toDepartment", toDepartment.Trim());
                cmd.Parameters.AddWithValue("@year", year.Trim());
                cmd.Parameters.AddWithValue("@semester", semester.Trim());
                cmd.Parameters.AddWithValue("@section", section.Trim());

                // 🔍 Log input to debug any issues
                Console.WriteLine($"📥 Dept: {toDepartment} | Year: {year} | Sem: {semester} | Sec: {section}");

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var day = reader["day"].ToString();
                    var hour = Convert.ToInt32(reader["hour"]);
                    var subject = reader["subject_code"].ToString();
                    var staff = reader["staff_assigned"].ToString();

                    if (!result.ContainsKey(day))
                        result[day] = new Dictionary<int, string>();

                    result[day][hour] = $"{subject} ({staff})";
                }

                // 🧱 Build full 5-day/7-hour table even with missing entries
                var days = new[] { "Mon", "Tue", "Wed", "Thu", "Fri" };
                var periods = Enumerable.Range(1, 7).ToList();

                var timetable = days.Select(day => new
                {
                    Day = day,
                    HourlySlots = periods.ToDictionary(
                        p => p,
                        p => result.ContainsKey(day) && result[day].ContainsKey(p) ? result[day][p] : "---"
                    )
                }).ToList();

                return Ok(new { timetable });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "❌ Internal Server Error while fetching timetable.",
                    error = ex.Message
                });
            }
        }

        [HttpGet("grouped1")]
        public async Task<IActionResult> GetAssignmentsForReceivingDepartment()
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                var results = new List<dynamic>();
                using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                var sql = @"
                    SELECT 
                        id,
                        from_department,
                        to_department,
                        subject_code,
                        subject_name,
                        year,
                        semester,
                        section,
                        assigned_at,
                        assigned_staff
                    FROM cross_department_assignments
                    ORDER BY year, semester, section, to_department
                ";

                using var cmd = new NpgsqlCommand(sql, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(new
                    {
                        id = Convert.ToInt32(reader["id"]),
                        fromDepartment = reader["from_department"].ToString(),
                        toDepartment = reader["to_department"].ToString(),
                        subCode = reader["subject_code"].ToString(),
                        subjectName = reader["subject_name"].ToString(),
                        year = reader["year"].ToString(),
                        semester = reader["semester"].ToString(),
                        section = reader["section"].ToString(),
                        assignedStaff = reader["assigned_staff"] == DBNull.Value ? null : reader["assigned_staff"].ToString()
                    });
                }

                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to retrieve data", error = ex.Message });
            }
        }

        [HttpGet("store")]
        public IActionResult StoreAssignment(
           string subCode,
           string subjectName,
           string subjectType,
           int credit,
           string staffAssigned,
           string year,
           string semester,
           string section,
           string department)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");

                using var conn = new NpgsqlConnection(connectionString);
                conn.Open();

                var query = @"
                    INSERT INTO subject_assignments
                    (sub_code, subject_name, subject_type, credit, staff_assigned, year, semester, section, department)
                    VALUES (@sub_code, @subject_name, @subject_type, @credit, @staff_assigned, @year, @semester, @section, @department)";

                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@sub_code", subCode);
                cmd.Parameters.AddWithValue("@subject_name", subjectName);
                cmd.Parameters.AddWithValue("@subject_type", subjectType);
                cmd.Parameters.AddWithValue("@credit", credit);
                cmd.Parameters.AddWithValue("@staff_assigned", staffAssigned ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@year", year);
                cmd.Parameters.AddWithValue("@semester", semester);
                cmd.Parameters.AddWithValue("@section", section);
                cmd.Parameters.AddWithValue("@department", department);

                cmd.ExecuteNonQuery();

                return Ok(new { message = "Assignment stored successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error storing assignment", error = ex.Message });
            }
        }
    }
}
