using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Microsoft.Extensions.Configuration;

namespace Timetablegenerator.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StaffRequestData : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public StaffRequestData(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("assignFromOtherDept")]
        public IActionResult AssignFromOtherDepartment([FromBody] AssignOtherDeptRequest req)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");

                using var conn = new NpgsqlConnection(connectionString);
                conn.Open();

                var query = @"
                    INSERT INTO cross_department_assignments 
                    (from_department, to_department, subject_code, subject_name, year, semester, section)
                    VALUES (@fromDept, @toDept, @subCode, @subName, @year, @semester, @section);
                ";

                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@fromDept", req.FromDepartment);
                cmd.Parameters.AddWithValue("@toDept", req.ToDepartment);
                cmd.Parameters.AddWithValue("@subCode", req.SubjectCode);
                cmd.Parameters.AddWithValue("@subName", req.SubjectName);
                cmd.Parameters.AddWithValue("@year", req.Year);
                cmd.Parameters.AddWithValue("@semester", req.Semester);
                cmd.Parameters.AddWithValue("@section", req.Section);

                int affectedRows = cmd.ExecuteNonQuery();

                return Ok(new { message = "Assignment saved successfully", rowsAffected = affectedRows });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Server error", error = ex.Message });
            }
        }
    }

    public class AssignOtherDeptRequest
    {
        public string FromDepartment { get; set; }
        public string ToDepartment { get; set; }
        public string SubjectCode { get; set; }
        public string SubjectName { get; set; }
        public string Year { get; set; }
        public string Semester { get; set; }
        public string Section { get; set; }
    }
}
