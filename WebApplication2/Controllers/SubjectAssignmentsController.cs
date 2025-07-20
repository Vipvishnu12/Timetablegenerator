using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TimetableGA;
using static TimetableGA.TimetableEngine;

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

     //   [HttpGet("generateCrossDepartmentTimetable")]
     //   public async Task<IActionResult> GenerateCrossDepartmentTimetable(
     //[FromQuery] string toDepartment,
     //[FromQuery] string year,
     //[FromQuery] string semester,
     //[FromQuery] string section)
     //   {
     //       var connectionString = _configuration.GetConnectionString("DefaultConnection");

     //       try
     //       {
     //           var subjects = new List<TimetableEngine.Subject>();
     //           var globalStaffAvailability = new Dictionary<string, Dictionary<string, HashSet<int>>>();

     //           using var conn = new NpgsqlConnection(connectionString);
     //           await conn.OpenAsync();

     //           // Step 1: Load Subject Assignments
     //           var query = @"
     //       SELECT * FROM subject_assignments
     //       WHERE LOWER(department) = LOWER(@toDepartment)
     //         AND LOWER(year) = LOWER(@year)
     //         AND LOWER(semester) = LOWER(@semester)
     //         AND LOWER(section) = LOWER(@section)
     //         AND staff_assigned IS NOT NULL AND TRIM(staff_assigned) <> ''
     //   ";

     //           using var cmd = new NpgsqlCommand(query, conn);
     //           cmd.Parameters.AddWithValue("@toDepartment", toDepartment.Trim());
     //           cmd.Parameters.AddWithValue("@year", year.Trim());
     //           cmd.Parameters.AddWithValue("@semester", semester.Trim());
     //           cmd.Parameters.AddWithValue("@section", section.Trim());

     //           using var reader = await cmd.ExecuteReaderAsync();
     //           while (await reader.ReadAsync())
     //           {
     //               subjects.Add(new TimetableEngine.Subject
     //               {
     //                   SubjectCode = reader["sub_code"]?.ToString() ?? "---",
     //                   SubjectName = reader["subject_name"]?.ToString() ?? "---",
     //                   SubjectType = reader["subject_type"]?.ToString() ?? "Theory",
     //                   Credit = Convert.ToInt32(reader["credit"]),
     //                   StaffAssigned = reader["staff_assigned"]?.ToString() ?? "---"
     //               });
     //           }
     //           reader.Close();

     //           if (subjects.Count == 0)
     //           {
     //               return BadRequest(new
     //               {
     //                   message = "❌ No valid subjects found. Ensure all have assigned staff.",
     //                   debug = new { toDepartment, year, semester, section }
     //               });
     //           }

     //           // Step 2: Load Staff Availability
     //           var availabilityQuery = "SELECT staff_assigned, day, hour FROM cross_class_timetable";
     //           using var existingCmd = new NpgsqlCommand(availabilityQuery, conn);
     //           using var availabilityReader = await existingCmd.ExecuteReaderAsync();
     //           while (await availabilityReader.ReadAsync())
     //           {
     //               var staff = availabilityReader["staff_assigned"].ToString();
     //               var day = availabilityReader["day"].ToString();
     //               var hour = Convert.ToInt32(availabilityReader["hour"]);

     //               if (!globalStaffAvailability.ContainsKey(staff))
     //               {
     //                   globalStaffAvailability[staff] = new Dictionary<string, HashSet<int>>();
     //                   foreach (var d in new[] { "Mon", "Tue", "Wed", "Thu", "Fri" })
     //                       globalStaffAvailability[staff][d] = new HashSet<int>();
     //               }

     //               if (!globalStaffAvailability[staff].ContainsKey(day))
     //                   globalStaffAvailability[staff][day] = new HashSet<int>();

     //               globalStaffAvailability[staff][day].Add(hour);
     //           }
     //           availabilityReader.Close();

     //           // Step 3: Generate Timetable
     //           var engine = new TimetableEngine();
     //           var (timetable, conflicts) = engine.Generate(subjects, globalStaffAvailability);

     //           // Step 4: Insert into cross_class_timetable and staff_timetable
     //           foreach (var daySlot in timetable)
     //           {
     //               foreach (var kv in daySlot.HourlySlots)
     //               {
     //                   string value = kv.Value;
     //                   if (value == "---") continue;

     //                   var hour = kv.Key;
     //                   var parts = value.Split("(", StringSplitOptions.TrimEntries);
     //                   string subjectCode = parts[0].Trim();
     //                   string rawStaffId = parts.Length > 1 ? parts[1].Replace(")", "").Trim() : "---";

     //                   // Insert into cross_class_timetable
     //                   var insertCmd = new NpgsqlCommand(@"
     //               INSERT INTO cross_class_timetable 
     //               (from_department, to_department, year, semester, section, day, hour, subject_code, staff_assigned)
     //               VALUES (@from_department, @to_department, @year, @semester, @section, @day, @hour, @subject_code, @staff_assigned);
     //           ", conn);

     //                   insertCmd.Parameters.AddWithValue("@from_department", toDepartment);
     //                   insertCmd.Parameters.AddWithValue("@to_department", toDepartment);
     //                   insertCmd.Parameters.AddWithValue("@year", year);
     //                   insertCmd.Parameters.AddWithValue("@semester", semester);
     //                   insertCmd.Parameters.AddWithValue("@section", section);
     //                   insertCmd.Parameters.AddWithValue("@day", daySlot.Day);
     //                   insertCmd.Parameters.AddWithValue("@hour", hour);
     //                   insertCmd.Parameters.AddWithValue("@subject_code", subjectCode);
     //                   insertCmd.Parameters.AddWithValue("@staff_assigned", rawStaffId);
     //                   await insertCmd.ExecuteNonQueryAsync();

     //                   var matchedSubject = subjects.FirstOrDefault(s => s.SubjectCode == subjectCode);
     //                   string fullStaff = matchedSubject?.StaffAssigned ?? "---";

     //                   string staffName = "---", staffId = "---";
     //                   if (fullStaff.Contains("("))
     //                   {
     //                       var nameIdParts = fullStaff.Split("(", StringSplitOptions.TrimEntries);
     //                       staffName = nameIdParts[0].Trim();
     //                       staffId = nameIdParts[1].Replace(")", "").Trim();
     //                   }

     //                   var staffInsertCmd = new NpgsqlCommand(@"
     //               INSERT INTO staff_timetable
     //               (staff_name, department, year, semester, section, day, hour, subject_code, subject_name, staff_id)
     //               VALUES
     //               (@staff_name, @department, @year, @semester, @section, @day, @hour, @subject_code, @subject_name, @staff_id);
     //           ", conn);

     //                   staffInsertCmd.Parameters.AddWithValue("@staff_name", staffName);
     //                   staffInsertCmd.Parameters.AddWithValue("@staff_id", staffId);
     //                   staffInsertCmd.Parameters.AddWithValue("@department", toDepartment);
     //                   staffInsertCmd.Parameters.AddWithValue("@year", year);
     //                   staffInsertCmd.Parameters.AddWithValue("@semester", semester);
     //                   staffInsertCmd.Parameters.AddWithValue("@section", section);
     //                   staffInsertCmd.Parameters.AddWithValue("@day", daySlot.Day);
     //                   staffInsertCmd.Parameters.AddWithValue("@hour", hour);
     //                   staffInsertCmd.Parameters.AddWithValue("@subject_code", subjectCode);
     //                   staffInsertCmd.Parameters.AddWithValue("@subject_name", matchedSubject?.SubjectName ?? "---");

     //                   await staffInsertCmd.ExecuteNonQueryAsync();
     //               }
     //           }

     //           // Step 5: Delete from source tables
     //           foreach (var subject in subjects)
     //           {
     //               var deleteSubjectCmd = new NpgsqlCommand(@"
     //           DELETE FROM subject_assignments
     //           WHERE LOWER(sub_code) = LOWER(@code)
     //             AND LOWER(department) = LOWER(@dept)
     //             AND LOWER(year) = LOWER(@year)
     //             AND LOWER(semester) = LOWER(@sem)
     //             AND LOWER(section) = LOWER(@sec)
     //       ", conn);
     //               deleteSubjectCmd.Parameters.AddWithValue("@code", subject.SubjectCode);
     //               deleteSubjectCmd.Parameters.AddWithValue("@dept", toDepartment);
     //               deleteSubjectCmd.Parameters.AddWithValue("@year", year);
     //               deleteSubjectCmd.Parameters.AddWithValue("@sem", semester);
     //               deleteSubjectCmd.Parameters.AddWithValue("@sec", section);
     //               await deleteSubjectCmd.ExecuteNonQueryAsync();

     //               var deleteCrossCmd = new NpgsqlCommand(@"
     //           DELETE FROM cross_department_assignments
     //           WHERE LOWER(subject_code) = LOWER(@code)
     //             AND LOWER(to_department) = LOWER(@dept)
     //             AND LOWER(year) = LOWER(@year)
     //             AND LOWER(semester) = LOWER(@sem)
     //             AND LOWER(section) = LOWER(@sec)
     //       ", conn);
     //               deleteCrossCmd.Parameters.AddWithValue("@code", subject.SubjectCode);
     //               deleteCrossCmd.Parameters.AddWithValue("@dept", toDepartment);
     //               deleteCrossCmd.Parameters.AddWithValue("@year", year);
     //               deleteCrossCmd.Parameters.AddWithValue("@sem", semester);
     //               deleteCrossCmd.Parameters.AddWithValue("@sec", section);
     //               await deleteCrossCmd.ExecuteNonQueryAsync();
     //           }

     //           return Ok(new
     //           {
     //               message = conflicts.Count == 0
     //                   ? "✅ Timetable generated and stored successfully."
     //                   : "⚠ Timetable generated with some conflicts. Stored valid entries.",
     //               timetable,
     //               conflicts = conflicts.Select(c => new
     //               {
     //                   subject = c.Subject.SubjectCode,
     //                   staff = c.Subject.StaffAssigned,
     //                   reason = c.Reason
     //               })
     //           });
     //       }
     //       catch (Exception ex)
     //       {
     //           return StatusCode(500, new
     //           {
     //               message = "❌ Internal Server Error while generating timetable.",
     //               error = ex.Message
     //           });
     //       }
     //   }

        // ✅ UPDATED BACKEND CONTROLLER METHOD
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
                var result = new Dictionary<string, Dictionary<int, (string Display, string SubjectId)>>();

                using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                var query = @"
            SELECT 
                cct.day, 
                cct.hour, 
                cct.subject_code, 
                cct.staff_assigned, 
                sd.subject_id
            FROM cross_class_timetable cct
            LEFT JOIN subject_data2 sd ON cct.subject_code = sd.sub_code
            WHERE cct.to_department = @toDepartment
              AND cct.year = @year
              AND cct.semester = @semester
              AND cct.section = @section
        ";

                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@toDepartment", toDepartment.Trim());
                cmd.Parameters.AddWithValue("@year", year.Trim());
                cmd.Parameters.AddWithValue("@semester", semester.Trim());
                cmd.Parameters.AddWithValue("@section", section.Trim());

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var day = reader["day"].ToString();
                    var hour = Convert.ToInt32(reader["hour"]);
                    var subjectCode = reader["subject_code"].ToString();
                    var staff = reader["staff_assigned"].ToString();
                    var subjectId = reader["subject_id"]?.ToString() ?? "0"; // VARCHAR support

                    var display = $"{subjectCode} ({staff})";

                    if (!result.ContainsKey(day))
                        result[day] = new Dictionary<int, (string, string)>();

                    result[day][hour] = (display, subjectId);
                }

                // Fill all 5 weekdays and 7 hours
                var days = new[] { "Mon", "Tue", "Wed", "Thu", "Fri" };
                var periods = Enumerable.Range(1, 7).ToList();

                var timetable = days.Select(day => new
                {
                    Day = day,
                    HourlySlots = periods.ToDictionary(
                        p => p,
                        p => result.ContainsKey(day) && result[day].ContainsKey(p)
                            ? new { display = result[day][p].Display, subjectId = result[day][p].SubjectId }
                            : new { display = "---", subjectId = "0" }
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
WHERE assigned_staff IS NULL OR TRIM(assigned_staff) = ''
ORDER BY 
    year,
    semester,
    section,
    to_department
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




        //[HttpPost("generateCrossDepartmentTimetable")]
        //public async Task<IActionResult> GenerateCrossDepartmentTimetable([FromBody] TimetableRequest request)
        //{
        //    var connectionString = _configuration.GetConnectionString("DefaultConnection");

        //    try
        //    {
        //        var subjects = new List<TimetableEngine.Subject>();
        //        var globalStaffAvailability = new Dictionary<string, Dictionary<string, HashSet<int>>>();

        //        // Load subjects directly from request
        //        foreach (var sub in request.Subjects)
        //        {
        //            if (string.IsNullOrWhiteSpace(sub.StaffAssigned)) continue;

        //            subjects.Add(new TimetableEngine.Subject
        //            {
        //                SubjectCode = sub.SubjectCode ?? "---",
        //                SubjectName = sub.SubjectName ?? "---",
        //                SubjectType = sub.SubjectType ?? "Theory",
        //                Credit = sub.Credit,
        //                StaffAssigned = sub.StaffAssigned
        //            });
        //        }

        //        if (subjects.Count == 0)
        //        {
        //            return BadRequest(new
        //            {
        //                message = "❌ No valid subjects found. Ensure all have assigned staff.",
        //                debug = request
        //            });
        //        }

        //        using var conn = new NpgsqlConnection(connectionString);
        //        await conn.OpenAsync();

        //        // Step 2: Load Staff Availability from existing timetable
        //        var availabilityQuery = "SELECT staff_assigned, day, hour FROM cross_class_timetable";
        //        using var existingCmd = new NpgsqlCommand(availabilityQuery, conn);
        //        using var availabilityReader = await existingCmd.ExecuteReaderAsync();

        //        while (await availabilityReader.ReadAsync())
        //        {
        //            var staff = availabilityReader["staff_assigned"].ToString();
        //            var day = availabilityReader["day"].ToString();
        //            var hour = Convert.ToInt32(availabilityReader["hour"]);

        //            if (!globalStaffAvailability.ContainsKey(staff))
        //            {
        //                globalStaffAvailability[staff] = new Dictionary<string, HashSet<int>>();
        //                foreach (var d in new[] { "Mon", "Tue", "Wed", "Thu", "Fri" })
        //                    globalStaffAvailability[staff][d] = new HashSet<int>();
        //            }

        //            if (!globalStaffAvailability[staff].ContainsKey(day))
        //                globalStaffAvailability[staff][day] = new HashSet<int>();

        //            globalStaffAvailability[staff][day].Add(hour);
        //        }
        //        availabilityReader.Close();

        //        // Step 3: Generate Timetable
        //        var engine = new TimetableEngine();
        //        var (timetable, conflicts) = engine.Generate(subjects, globalStaffAvailability);

        //        // Step 4: Save into cross_class_timetable and staff_timetable
        //        foreach (var daySlot in timetable)
        //        {
        //            foreach (var kv in daySlot.HourlySlots)
        //            {
        //                string value = kv.Value;
        //                if (value == "---") continue;

        //                var hour = kv.Key;
        //                var parts = value.Split("(", StringSplitOptions.TrimEntries);
        //                string subjectCode = parts[0].Trim();
        //                string rawStaffId = parts.Length > 1 ? parts[1].Replace(")", "").Trim() : "---";

        //                // Save to cross_class_timetable
        //                var insertCmd = new NpgsqlCommand(@"
        //            INSERT INTO cross_class_timetable 
        //            (from_department, to_department, year, semester, section, day, hour, subject_code, staff_assigned)
        //            VALUES (@from_department, @to_department, @year, @semester, @section, @day, @hour, @subject_code, @staff_assigned);
        //        ", conn);

        //                insertCmd.Parameters.AddWithValue("@from_department", request.Department);
        //                insertCmd.Parameters.AddWithValue("@to_department", request.Department);
        //                insertCmd.Parameters.AddWithValue("@year", request.Year);
        //                insertCmd.Parameters.AddWithValue("@semester", request.Semester);
        //                insertCmd.Parameters.AddWithValue("@section", request.Section);
        //                insertCmd.Parameters.AddWithValue("@day", daySlot.Day);
        //                insertCmd.Parameters.AddWithValue("@hour", hour);
        //                insertCmd.Parameters.AddWithValue("@subject_code", subjectCode);
        //                insertCmd.Parameters.AddWithValue("@staff_assigned", rawStaffId);

        //                await insertCmd.ExecuteNonQueryAsync();

        //                // Now insert into staff_timetable (with staff_id + name)
        //                var matchedSubject = subjects.FirstOrDefault(s => s.SubjectCode == subjectCode);
        //                string fullStaff = matchedSubject?.StaffAssigned ?? "---";

        //                string staffName = "---", staffId = "---";
        //                if (fullStaff.Contains("("))
        //                {
        //                    var nameIdParts = fullStaff.Split("(", StringSplitOptions.TrimEntries);
        //                    staffName = nameIdParts[0].Trim();
        //                    staffId = nameIdParts[1].Replace(")", "").Trim();
        //                }

        //                var staffInsertCmd = new NpgsqlCommand(@"
        //            INSERT INTO staff_timetable
        //            (staff_name, staff_id, department, year, semester, section, day, hour, subject_code, subject_name)
        //            VALUES
        //            (@staff_name, @staff_id, @department, @year, @semester, @section, @day, @hour, @subject_code, @subject_name);
        //        ", conn);

        //                staffInsertCmd.Parameters.AddWithValue("@staff_name", staffName);
        //                staffInsertCmd.Parameters.AddWithValue("@staff_id", staffId);
        //                staffInsertCmd.Parameters.AddWithValue("@department", request.Department);
        //                staffInsertCmd.Parameters.AddWithValue("@year", request.Year);
        //                staffInsertCmd.Parameters.AddWithValue("@semester", request.Semester);
        //                staffInsertCmd.Parameters.AddWithValue("@section", request.Section);
        //                staffInsertCmd.Parameters.AddWithValue("@day", daySlot.Day);
        //                staffInsertCmd.Parameters.AddWithValue("@hour", hour);
        //                staffInsertCmd.Parameters.AddWithValue("@subject_code", subjectCode);
        //                staffInsertCmd.Parameters.AddWithValue("@subject_name", matchedSubject?.SubjectName ?? "---");

        //                await staffInsertCmd.ExecuteNonQueryAsync();
        //            }
        //        }

        //        return Ok(new
        //        {
        //            message = conflicts.Count == 0
        //                ? "✅ Timetable generated and stored successfully."
        //                : "⚠ Timetable generated with some conflicts. Stored valid entries.",
        //            timetable,
        //            conflicts = conflicts.Select(c => new
        //            {
        //                subject = c.Subject.SubjectCode,
        //                staff = c.Subject.StaffAssigned,
        //                reason = c.Reason
        //            })
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            message = "❌ Internal Server Error while generating timetable.",
        //            error = ex.Message
        //        });
        //    }
        //}




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
     string department,
     string? labId = null // ✅ Added labId
  )
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");

                using var conn = new NpgsqlConnection(connectionString);
                conn.Open();

                var query = @"
            INSERT INTO subject_assignments
            (sub_code, subject_name, subject_type, credit, staff_assigned, year, semester, section, department, lab_id)
            VALUES (@sub_code, @subject_name, @subject_type, @credit, @staff_assigned, @year, @semester, @section, @department, @lab_id)";

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
                cmd.Parameters.AddWithValue("@lab_id", (object?)labId ?? DBNull.Value); // ✅ Handle nulls

                cmd.ExecuteNonQuery();

                return Ok(new { message = "Assignment stored successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error storing assignment", error = ex.Message });
            }
        }

        [HttpGet("getStaffTimetableById")]
        public async Task<IActionResult> GetStaffTimetableById([FromQuery] string staffId)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                string staffName = "---";
                var resultList = new List<object>();

                // join on sub_code (not subject_code) in subject_data2
                var query = @"
            SELECT
                st.staff_name,
                st.department,
                st.year,
                st.section,
                st.day,
                st.hour,
                st.subject_code,
                sd.subject_id
            FROM staff_timetable AS st
            LEFT JOIN subject_data2 AS sd
              ON LOWER(st.subject_code) = LOWER(sd.sub_code)
            WHERE LOWER(st.staff_id) = LOWER(@staffId)
            ORDER BY st.day, st.hour;
        ";

                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@staffId", staffId.Trim());

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    // capture the first occurrence of staff_name
                    if (staffName == "---")
                        staffName = reader["staff_name"]?.ToString() ?? "---";

                    resultList.Add(new
                    {
                        Department = reader["department"].ToString(),
                        Year = reader["year"].ToString(),
                        Section = reader["section"].ToString(),
                        Day = reader["day"].ToString(),
                        Hour = Convert.ToInt32(reader["hour"]),
                        SubjectCode = reader["subject_code"].ToString(),
                        SubjectId = reader["subject_id"] == DBNull.Value
                                          ? ""
                                          : reader["subject_id"].ToString()
                    });
                }

                if (resultList.Count == 0)
                {
                    return NotFound(new
                    {
                        message = "❌ No timetable found for given staff ID.",
                        staffId
                    });
                }

                return Ok(new
                {
                    staffId,
                    staffName,
                    records = resultList
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "❌ Error occurred while fetching staff timetable.",
                    error = ex.Message
                });
            }
        }

        [HttpGet("periods")]
        public async Task<IActionResult> GetStaffSubjectPeriodCounts()
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            var result = new List<object>();
            var query = @"
    SELECT 
        staff_id, 
        staff_name, 
        department,
        subject_code, 
        subject_name, 
        COUNT(*) AS period_count
    FROM staff_timetable
    GROUP BY staff_id, staff_name, department, subject_code, subject_name
    ORDER BY staff_id, subject_name;
";

            try
            {
                using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                using var cmd = new NpgsqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    result.Add(new
                    {
                        staffId = reader["staff_id"].ToString(),
                        staffName = reader["staff_name"].ToString(),
                        department = reader["department"].ToString(),
                        subjectCode = reader["subject_code"].ToString(),
                        subjectName = reader["subject_name"].ToString(),
                        count = Convert.ToInt32(reader["period_count"])  // Safe conversion from Int64 to Int32
                    });
                }

                return Ok(result);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "❌ Failed to retrieve staff period counts",
                    error = ex.Message
                });
            }




        }



        [HttpPut("update")]
        public async Task<IActionResult> UpdateStaff([FromBody] StaffDto staff)
        {
            var connString = _configuration.GetConnectionString("DefaultConnection");
            try
            {
                using var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync();

                var query = @"
            UPDATE staff_data2 SET
              name = @name,
              subject1 = @subject1,
              subject2 = @subject2,
              subject3 = @subject3
            WHERE staff_id = @staffId;
        ";

                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@name", staff.name);
                cmd.Parameters.AddWithValue("@subject1", staff.subject1 ?? "");
                cmd.Parameters.AddWithValue("@subject2", staff.subject2 ?? "");
                cmd.Parameters.AddWithValue("@subject3", staff.subject3 ?? "");
                cmd.Parameters.AddWithValue("@staffId", staff.staffId);

                int affected = await cmd.ExecuteNonQueryAsync();
                return Ok(new { message = affected > 0 ? "✅ Updated" : "❌ Not found" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "❌ Error updating staff", error = ex.Message });
            }


        }
     
        
        [HttpPost("generateCrossDepartmentTimetable")]
        public async Task<IActionResult> GenerateCrossDepartmentTimetable([FromBody] TimetableRequest request)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                var subjects = new List<TimetableEngine.Subject>();
                var globalStaffAvailability = new Dictionary<string, Dictionary<string, HashSet<int>>>();
                var labAvailability = new Dictionary<string, Dictionary<string, HashSet<int>>>();
                var labTimetable = new List<object>();
                var debugLogs = new List<string>();

                using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                // Build subjects list directly from frontend data (no dummy lab ID logic)
                foreach (var sub in request.Subjects)
                {
                    if (string.IsNullOrWhiteSpace(sub.StaffAssigned)) continue;

                    debugLogs.Add($"📘 Subject received: {sub.SubjectCode} - {sub.SubjectName} ({sub.SubjectType})");

                    subjects.Add(new TimetableEngine.Subject
                    {
                        SubjectCode = sub.SubjectCode ?? "---",
                        SubjectName = sub.SubjectName ?? "---",
                        SubjectType = sub.SubjectType ?? "Theory",
                        Credit = sub.Credit,
                        StaffAssigned = sub.StaffAssigned,
                        LabId = sub.SubjectType?.ToLower() == "lab" ? sub.LabId?.Trim() : null
                    });
                }

                if (subjects.Count == 0)
                {
                    return BadRequest(new
                    {
                        message = "❌ No valid subjects found. Ensure all have assigned staff.",
                        receivedPayload = request
                    });
                }

                // Load Staff Availability
                var availabilityQuery = "SELECT staff_assigned, day, hour FROM cross_class_timetable";
                using var existingCmd = new NpgsqlCommand(availabilityQuery, conn);
                using var reader = await existingCmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var staff = reader["staff_assigned"].ToString();
                    var day = reader["day"].ToString();
                    var hour = Convert.ToInt32(reader["hour"]);

                    if (!globalStaffAvailability.ContainsKey(staff))
                    {
                        globalStaffAvailability[staff] = new();
                        foreach (var d in new[] { "Mon", "Tue", "Wed", "Thu", "Fri" })
                            globalStaffAvailability[staff][d] = new();
                    }
                    globalStaffAvailability[staff][day].Add(hour);
                }
                reader.Close();

                // Generate Timetable
                var engine = new TimetableEngine();
                var (timetable, conflicts) = engine.Generate(subjects, globalStaffAvailability, labAvailability);

                // Insert timetable entries
                foreach (var daySlot in timetable)
                {
                    foreach (var kv in daySlot.HourlySlots)
                    {
                        string value = kv.Value;
                        if (value == "---") continue;

                        var hour = kv.Key;
                        var parts = value.Split("(", StringSplitOptions.TrimEntries);
                        string subjectCode = parts[0].Trim();
                        string rawStaffId = parts.Length > 1 ? parts[1].Replace(")", "").Trim() : "---";

                        var matchedSubject = subjects.FirstOrDefault(s => s.SubjectCode.Trim() == subjectCode);
                        string fullStaff = matchedSubject?.StaffAssigned ?? "---";

                        string staffName = "---", staffId = "---";
                        if (fullStaff.Contains("("))
                        {
                            var nameId = fullStaff.Split("(", StringSplitOptions.TrimEntries);
                            staffName = nameId[0].Trim();
                            staffId = nameId[1].Replace(")", "").Trim();
                        }

                        // Insert into cross_class_timetable
                        var insertCmd = new NpgsqlCommand(@"
                    INSERT INTO cross_class_timetable 
                    (from_department, to_department, year, semester, section, day, hour, subject_code, staff_assigned)
                    VALUES (@from_department, @to_department, @year, @semester, @section, @day, @hour, @subject_code, @staff_assigned);", conn);

                        insertCmd.Parameters.AddWithValue("@from_department", request.Department);
                        insertCmd.Parameters.AddWithValue("@to_department", request.Department);
                        insertCmd.Parameters.AddWithValue("@year", request.Year);
                        insertCmd.Parameters.AddWithValue("@semester", request.Semester);
                        insertCmd.Parameters.AddWithValue("@section", request.Section);
                        insertCmd.Parameters.AddWithValue("@day", daySlot.Day);
                        insertCmd.Parameters.AddWithValue("@hour", hour);
                        insertCmd.Parameters.AddWithValue("@subject_code", subjectCode);
                        insertCmd.Parameters.AddWithValue("@staff_assigned", rawStaffId);
                        await insertCmd.ExecuteNonQueryAsync();

                        // Insert into staff_timetable
                        var staffInsertCmd = new NpgsqlCommand(@"
                    INSERT INTO staff_timetable
                    (staff_name, staff_id, department, year, semester, section, day, hour, subject_code, subject_name)
                    VALUES
                    (@staff_name, @staff_id, @department, @year, @semester, @section, @day, @hour, @subject_code, @subject_name);", conn);

                        staffInsertCmd.Parameters.AddWithValue("@staff_name", staffName);
                        staffInsertCmd.Parameters.AddWithValue("@staff_id", staffId);
                        staffInsertCmd.Parameters.AddWithValue("@department", request.Department);
                        staffInsertCmd.Parameters.AddWithValue("@year", request.Year);
                        staffInsertCmd.Parameters.AddWithValue("@semester", request.Semester);
                        staffInsertCmd.Parameters.AddWithValue("@section", request.Section);
                        staffInsertCmd.Parameters.AddWithValue("@day", daySlot.Day);
                        staffInsertCmd.Parameters.AddWithValue("@hour", hour);
                        staffInsertCmd.Parameters.AddWithValue("@subject_code", subjectCode);
                        staffInsertCmd.Parameters.AddWithValue("@subject_name", matchedSubject?.SubjectName ?? "---");
                        await staffInsertCmd.ExecuteNonQueryAsync();

                        // Lab entry
                        if (matchedSubject?.SubjectType?.Trim().ToLower() == "lab" && !string.IsNullOrEmpty(matchedSubject.LabId))
                        {
                            var labInsertCmd = new NpgsqlCommand(@"
                        INSERT INTO lab_timetable
                        (lab_id, subject_code, subject_name, staff_assigned, department, year, semester, section, day, hour)
                        VALUES
                        (@lab_id, @subject_code, @subject_name, @staff_assigned, @department, @year, @semester, @section, @day, @hour);", conn);

                            labInsertCmd.Parameters.AddWithValue("@lab_id", matchedSubject.LabId);
                            labInsertCmd.Parameters.AddWithValue("@subject_code", matchedSubject.SubjectCode);
                            labInsertCmd.Parameters.AddWithValue("@subject_name", matchedSubject.SubjectName);
                            labInsertCmd.Parameters.AddWithValue("@staff_assigned", matchedSubject.StaffAssigned);
                            labInsertCmd.Parameters.AddWithValue("@department", request.Department);
                            labInsertCmd.Parameters.AddWithValue("@year", request.Year);
                            labInsertCmd.Parameters.AddWithValue("@semester", request.Semester);
                            labInsertCmd.Parameters.AddWithValue("@section", request.Section);
                            labInsertCmd.Parameters.AddWithValue("@day", daySlot.Day);
                            labInsertCmd.Parameters.AddWithValue("@hour", hour);
                            await labInsertCmd.ExecuteNonQueryAsync();

                            labTimetable.Add(new
                            {
                                lab_id = matchedSubject.LabId,
                                subject_code = matchedSubject.SubjectCode,
                                subject_name = matchedSubject.SubjectName,
                                staff_assigned = matchedSubject.StaffAssigned,
                                department = request.Department,
                                year = request.Year,
                                semester = request.Semester,
                                section = request.Section,
                                day = daySlot.Day,
                                hour = hour
                            });
                        }
                    }
                }

                return Ok(new
                {
                    message = conflicts.Count == 0
          ? "✅ Timetable generated and stored successfully."
          : "⚠ Timetable generated with some conflicts. Stored valid entries.",
                    timetable,
                    labTimetable,
                    usedLabIds = subjects
          .Select(s => s.LabId)
          .Distinct()
          .ToList(),
                    receivedPayload = request,
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
                var labAvailability = new Dictionary<string, Dictionary<string, HashSet<int>>>();
                var labTimetable = new List<object>();

                using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                var query = @"
            SELECT * FROM subject_assignments
            WHERE LOWER(department) = LOWER(@toDepartment)
              AND LOWER(year) = LOWER(@year)
              AND LOWER(semester) = LOWER(@semester)
              AND LOWER(section) = LOWER(@section)
              AND staff_assigned IS NOT NULL AND TRIM(staff_assigned) <> ''";

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
                        StaffAssigned = reader["staff_assigned"]?.ToString() ?? "---",
                        LabId = reader["lab_id"]?.ToString()
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

                var availabilityQuery = "SELECT staff_assigned, day, hour FROM cross_class_timetable";
                using var existingCmd = new NpgsqlCommand(availabilityQuery, conn);
                using var availabilityReader = await existingCmd.ExecuteReaderAsync();
                while (await availabilityReader.ReadAsync())
                {
                    var staff = availabilityReader["staff_assigned"].ToString();
                    var day = availabilityReader["day"].ToString();
                    var hour = Convert.ToInt32(availabilityReader["hour"]);

                    if (!globalStaffAvailability.ContainsKey(staff))
                    {
                        globalStaffAvailability[staff] = new();
                        foreach (var d in new[] { "Mon", "Tue", "Wed", "Thu", "Fri" })
                            globalStaffAvailability[staff][d] = new();
                    }

                    globalStaffAvailability[staff][day].Add(hour);
                }
                availabilityReader.Close();

                var engine = new TimetableEngine();
                var (timetable, conflicts) = engine.Generate(subjects, globalStaffAvailability, labAvailability);

                foreach (var daySlot in timetable)
                {
                    foreach (var kv in daySlot.HourlySlots)
                    {
                        string value = kv.Value;
                        if (value == "---") continue;

                        var hour = kv.Key;
                        var parts = value.Split("(", StringSplitOptions.TrimEntries);
                        string subjectCode = parts[0].Trim();
                        string rawStaffId = parts.Length > 1 ? parts[1].Replace(")", "").Trim() : "---";

                        var matchedSubject = subjects.FirstOrDefault(s => s.SubjectCode == subjectCode);
                        string fullStaff = matchedSubject?.StaffAssigned ?? "---";

                        string staffName = "---", staffId = "---";
                        if (fullStaff.Contains("("))
                        {
                            var nameIdParts = fullStaff.Split("(", StringSplitOptions.TrimEntries);
                            staffName = nameIdParts[0].Trim();
                            staffId = nameIdParts[1].Replace(")", "").Trim();
                        }

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
                        insertCmd.Parameters.AddWithValue("@staff_assigned", rawStaffId);
                        await insertCmd.ExecuteNonQueryAsync();

                        var staffInsertCmd = new NpgsqlCommand(@"
                    INSERT INTO staff_timetable
                    (staff_name, department, year, semester, section, day, hour, subject_code, subject_name, staff_id)
                    VALUES
                    (@staff_name, @department, @year, @semester, @section, @day, @hour, @subject_code, @subject_name, @staff_id);
                ", conn);

                        staffInsertCmd.Parameters.AddWithValue("@staff_name", staffName);
                        staffInsertCmd.Parameters.AddWithValue("@staff_id", staffId);
                        staffInsertCmd.Parameters.AddWithValue("@department", toDepartment);
                        staffInsertCmd.Parameters.AddWithValue("@year", year);
                        staffInsertCmd.Parameters.AddWithValue("@semester", semester);
                        staffInsertCmd.Parameters.AddWithValue("@section", section);
                        staffInsertCmd.Parameters.AddWithValue("@day", daySlot.Day);
                        staffInsertCmd.Parameters.AddWithValue("@hour", hour);
                        staffInsertCmd.Parameters.AddWithValue("@subject_code", subjectCode);
                        staffInsertCmd.Parameters.AddWithValue("@subject_name", matchedSubject?.SubjectName ?? "---");
                        await staffInsertCmd.ExecuteNonQueryAsync();

                        if (matchedSubject?.SubjectType?.Trim().ToLower() == "lab" && !string.IsNullOrEmpty(matchedSubject.LabId))
                        {
                            var labInsertCmd = new NpgsqlCommand(@"
                        INSERT INTO lab_timetable
                        (lab_id, subject_code, subject_name, staff_assigned, department, year, semester, section, day, hour)
                        VALUES
                        (@lab_id, @subject_code, @subject_name, @staff_assigned, @department, @year, @semester, @section, @day, @hour);
                    ", conn);

                            labInsertCmd.Parameters.AddWithValue("@lab_id", matchedSubject.LabId);
                            labInsertCmd.Parameters.AddWithValue("@subject_code", matchedSubject.SubjectCode);
                            labInsertCmd.Parameters.AddWithValue("@subject_name", matchedSubject.SubjectName);
                            labInsertCmd.Parameters.AddWithValue("@staff_assigned", matchedSubject.StaffAssigned);
                            labInsertCmd.Parameters.AddWithValue("@department", toDepartment);
                            labInsertCmd.Parameters.AddWithValue("@year", year);
                            labInsertCmd.Parameters.AddWithValue("@semester", semester);
                            labInsertCmd.Parameters.AddWithValue("@section", section);
                            labInsertCmd.Parameters.AddWithValue("@day", daySlot.Day);
                            labInsertCmd.Parameters.AddWithValue("@hour", hour);
                            await labInsertCmd.ExecuteNonQueryAsync();

                            labTimetable.Add(new
                            {
                                lab_id = matchedSubject.LabId,
                                subject_code = matchedSubject.SubjectCode,
                                subject_name = matchedSubject.SubjectName,
                                staff_assigned = matchedSubject.StaffAssigned,
                                department = toDepartment,
                                year,
                                semester,
                                section,
                                day = daySlot.Day,
                                hour
                            });
                        }
                    }
                }

                return Ok(new
                {
                    message = conflicts.Count == 0
                        ? "✅ Timetable generated and stored successfully."
                        : "⚠ Timetable generated with some conflicts. Stored valid entries.",
                    timetable,
                    labTimetable,
                    usedLabIds = subjects.Select(s => s.LabId).Distinct().ToList(),
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


        public class TimetableRequest
        {
            [JsonPropertyName("department")]
            public string Department { get; set; }

            [JsonPropertyName("year")]
            public string Year { get; set; }

            [JsonPropertyName("semester")]
            public string Semester { get; set; }

            [JsonPropertyName("section")]
            public string Section { get; set; }

            [JsonPropertyName("subjects")]
            public List<SubjectDto> Subjects { get; set; }
        }

        public class SubjectDto
        {
            [JsonPropertyName("subjectCode")]
            public string SubjectCode { get; set; }

            [JsonPropertyName("subjectName")]
            public string SubjectName { get; set; }

            [JsonPropertyName("subjectType")]
            public string SubjectType { get; set; }

            [JsonPropertyName("credit")]
            public int Credit { get; set; }

            [JsonPropertyName("staffAssigned")]
            public string StaffAssigned { get; set; }

            [JsonPropertyName("labId")]
            public string LabId { get; set; }
        }




public class StaffDto
        {
            public string? department { get; set; }
            public string? department_id { get; set; }
            public string? block { get; set; }
            public string staffId { get; set; }
            public string name { get; set; }
            public string? subject1 { get; set; }
            public string? subject2 { get; set; }
            public string? subject3 { get; set; }
        }





        //public class TimetableRequest
        //{
        //    public string Department { get; set; }
        //    public string Year { get; set; }
        //    public string Semester { get; set; }
        //    public string Section { get; set; }

        //    public List<SubjectInput> Subjects { get; set; }

        //    public class SubjectInput
        //    {
        //        public string SubjectCode { get; set; }
        //        public string SubjectName { get; set; }
        //        public string SubjectType { get; set; }
        //        public int Credit { get; set; }
        //        public string StaffAssigned { get; set; }
        //    }
        //}



    }
}
