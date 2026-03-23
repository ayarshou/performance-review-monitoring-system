using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerformanceReviewApi.Data;
using PerformanceReviewApi.Helpers;

namespace PerformanceReviewApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    public AuthController(AppDbContext db) => _db = db;

    // POST /api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Username and password are required." });

        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Username == request.Username.Trim().ToLower());

        if (employee?.PasswordHash is null || !PasswordHelper.Verify(request.Password, employee.PasswordHash))
            return Unauthorized(new { message = "Invalid username or password." });

        // Return only safe fields — PasswordHash is intentionally excluded
        return Ok(new
        {
            id        = employee.Id,
            name      = employee.Name,
            email     = employee.Email,
            position  = employee.Position,
            managerId = employee.ManagerId,
        });
    }
}

public record LoginRequest(string Username, string Password);
