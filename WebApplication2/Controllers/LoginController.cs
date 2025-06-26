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

        [HttpPost("verify")]
        public IActionResult Verify([FromBody] LoginDto dto)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");

                using var conn = new NpgsqlConnection(connectionString);
                conn.Open();

                var query = "SELECT * FROM login WHERE username = @username AND password = @password";
                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@username", dto.Username);
                cmd.Parameters.AddWithValue("@password", dto.Password); // use hashed passwords in real apps

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return Ok(new { message = "Login successful", username = dto.Username });
                }
                else
                {
                    return Unauthorized(new { message = "Invalid credentials" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Server error", error = ex.Message });
            }
        }
    }

    public class LoginDto
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
