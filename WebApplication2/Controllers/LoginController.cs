using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Microsoft.Extensions.Configuration;

namespace YourNamespace.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoginController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public LoginController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // ✅ Login verification
        [HttpPost("verify")]
        public IActionResult Verify([FromBody] LoginDto dto)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");

                using var conn = new NpgsqlConnection(connectionString);
                conn.Open();

                var query = @"
            SELECT department_name 
            FROM admin 
            WHERE department_id = @username AND password = @password
        ";

                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@username", dto.Username.ToUpper());
                cmd.Parameters.AddWithValue("@password", dto.Password);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    var deptName = reader.GetString(0);
                    return Ok(new
                    {
                        message = "Login successful",
                        username = dto.Username.ToUpper(),
                        departmentName = deptName
                    });
                }
                else
                {
                    return Unauthorized(new { message = "Invalid department ID or password." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Server error", error = ex.Message });
            }
        }

        // ✅ Insert into admin table instead of department
        [HttpPost("add-department")]
        public IActionResult AddDepartment([FromBody] DepartmentDto dept)
        {
            if (dept == null || string.IsNullOrWhiteSpace(dept.DepartmentId) ||
                string.IsNullOrWhiteSpace(dept.DepartmentName) ||
                string.IsNullOrWhiteSpace(dept.Block) ||
                string.IsNullOrWhiteSpace(dept.Password))
            {
                return BadRequest(new { message = "All fields are required." });
            }

            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");

                using var conn = new NpgsqlConnection(connectionString);
                conn.Open();

                var query = @"
                    INSERT INTO admin (department_id, department_name, block, password)
                    VALUES (@DepartmentId, @DepartmentName, @Block, @Password)
                ";

                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@DepartmentId", dept.DepartmentId.ToUpper());
                cmd.Parameters.AddWithValue("@DepartmentName", dept.DepartmentName.ToUpper());
                cmd.Parameters.AddWithValue("@Block", dept.Block.ToUpper());
                cmd.Parameters.AddWithValue("@Password", dept.Password);

                cmd.ExecuteNonQuery();

                return Ok(new { message = "Admin department added successfully." });
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                return Conflict(new { message = "Department already exists." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Server error", error = ex.Message });
            }
        }
    }

    // DTOs
    public class LoginDto
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class DepartmentDto
    {
        public string DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public string Block { get; set; }
        public string Password { get; set; }
    }
}
