using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Timetablegenerator.Connection;
using System.Collections.Generic;

namespace Timetablegenerator.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StaffDataController : ControllerBase
    {
        private readonly DatabaseConnection _db;

        public StaffDataController(DatabaseConnection db)
        {
            _db = db;
        }

        // ✅ POST: Add staff list
        [HttpPost("add")]
        public IActionResult AddStaff([FromBody] List<StaffDataDto> staffList)
        {
            if (staffList == null || staffList.Count == 0)
                return BadRequest(new { message = "Staff list is empty" });

            using var conn = _db.GetConnection();
            conn.Open();
            using var transaction = conn.BeginTransaction();

            try
            {
                foreach (var staff in staffList)
                {
                    string query = @"
                        INSERT INTO staff_data2
                        (department, department_id, block, staff_id, name, subject1, subject2, subject3)
                        VALUES 
                        (@department, @department_id, @block, @staff_id, @name, @subject1, @subject2, @subject3);";

                    using var cmd = new NpgsqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@department", staff.department ?? "");
                    cmd.Parameters.AddWithValue("@department_id", staff.department_id ?? "");
                    cmd.Parameters.AddWithValue("@block", staff.block ?? "");
                    cmd.Parameters.AddWithValue("@staff_id", staff.staffId ?? "");
                    cmd.Parameters.AddWithValue("@name", staff.name ?? "");
                    cmd.Parameters.AddWithValue("@subject1", staff.subject1 ?? "");
                    cmd.Parameters.AddWithValue("@subject2", staff.subject2 ?? "");
                    cmd.Parameters.AddWithValue("@subject3", staff.subject3 ?? "");
                    cmd.ExecuteNonQuery();
                }

                transaction.Commit();
                return Ok(new { message = "Staff data added successfully" });
            }
            catch (PostgresException pgEx)
            {
                transaction.Rollback();
                return StatusCode(500, new { message = "PostgreSQL error", error = pgEx.Message });
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return StatusCode(500, new { message = "Server error", error = ex.Message });
            }
        }

        // ✅ GET: Staff count for department
        [HttpGet("count/{departmentId}")]
        public IActionResult GetStaffCountByDepartment(string departmentId)
        {
            if (string.IsNullOrWhiteSpace(departmentId))
                return BadRequest(new { message = "Department ID is required" });

            try
            {
                using var conn = _db.GetConnection();
                conn.Open();

                string query = "SELECT COUNT(*) FROM staff_data2 WHERE department_id = @dept";
                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@dept", departmentId);

                int count = Convert.ToInt32(cmd.ExecuteScalar());
                return Ok(new { count = count });
            }
            catch (PostgresException pgEx)
            {
                return StatusCode(500, new { message = "PostgreSQL error", error = pgEx.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Server error", error = ex.Message });
            }
        }

        // ✅ NEW: GET staff list by department_id
        [HttpGet("department/{departmentId}")]
        public IActionResult GetStaffByDepartment(string departmentId)
        {
            if (string.IsNullOrWhiteSpace(departmentId))
                return BadRequest(new { message = "Department ID is required" });

            try
            {
                using var conn = _db.GetConnection();
                conn.Open();

                string query = @"
                    SELECT staff_id, name, subject1, subject2, subject3 
                    FROM staff_data2 
                    WHERE department_id = @dept";

                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@dept", departmentId);

                var staffList = new List<StaffDataDto>();

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    staffList.Add(new StaffDataDto
                    {
                        staffId = reader["staff_id"]?.ToString(),
                        name = reader["name"]?.ToString(),
                        subject1 = reader["subject1"]?.ToString(),
                        subject2 = reader["subject2"]?.ToString(),
                        subject3 = reader["subject3"]?.ToString(),
                    });
                }

                return Ok(staffList);
            }
            catch (PostgresException pgEx)
            {
                return StatusCode(500, new { message = "PostgreSQL error", error = pgEx.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Server error", error = ex.Message });
            }
        }
    }

    // DTO class
    public class StaffDataDto
    {
        public string department { get; set; }
        public string department_id { get; set; }
        public string block { get; set; }
        public string staffId { get; set; }
        public string name { get; set; }
        public string subject1 { get; set; }
        public string subject2 { get; set; }
        public string subject3 { get; set; }
    }
}
