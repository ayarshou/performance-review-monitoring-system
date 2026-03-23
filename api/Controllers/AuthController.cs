using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using PerformanceReviewApi.Data;
using PerformanceReviewApi.Helpers;
using PerformanceReviewApi.Models;

namespace PerformanceReviewApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;

    [ActivatorUtilitiesConstructor]
    public AuthController(AppDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    public AuthController(AppDbContext db)
        : this(db, BuildFallbackConfiguration())
    {
    }

    // POST /api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Username and password are required." });

        var normalizedUsername = request.Username.Trim().ToLowerInvariant();

        var user = await _db.Users
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Username.ToLower() == normalizedUsername);

        if (user is not null && PasswordHashHelper.Verify(request.Password, user.PasswordHash))
        {
            return Ok(CreateLoginResponse(
                username: user.Username,
                role: user.Role,
                employeeId: user.EmployeeId,
                name: user.Employee?.Name ?? user.Username,
                email: user.Employee?.Email,
                position: user.Employee?.Position,
                managerId: user.Employee?.ManagerId,
                responseId: user.EmployeeId ?? user.Id));
        }

        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Username != null && e.Username.ToLower() == normalizedUsername);

        if (employee?.PasswordHash is null || !PasswordHelper.Verify(request.Password, employee.PasswordHash))
            return Unauthorized(new { message = "Invalid username or password." });

        var role = await _db.Employees.AnyAsync(e => e.ManagerId == employee.Id)
            ? "Manager"
            : "Employee";

        return Ok(CreateLoginResponse(
            username: employee.Username ?? normalizedUsername,
            role: role,
            employeeId: employee.Id,
            name: employee.Name,
            email: employee.Email,
            position: employee.Position,
            managerId: employee.ManagerId,
            responseId: employee.Id));
    }

    private object CreateLoginResponse(
        string username,
        string role,
        int? employeeId,
        string name,
        string? email,
        string? position,
        int? managerId,
        int responseId)
    {
        var token = CreateToken(username, name, role, employeeId);

        return new
        {
            token,
            id = responseId,
            name,
            email,
            position,
            managerId,
            role,
        };
    }

    private string CreateToken(string username, string displayName, string role, int? employeeId)
    {
        var secretKey = _configuration["JwtSettings:SecretKey"]
            ?? throw new InvalidOperationException("JwtSettings:SecretKey must be configured.");
        var issuer = _configuration["JwtSettings:Issuer"] ?? "PerformanceReviewApi";
        var audience = _configuration["JwtSettings:Audience"] ?? "PerformanceReviewClient";
        var expiresInHours = double.TryParse(_configuration["JwtSettings:ExpiresInHours"], out var parsed)
            ? parsed
            : 8;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, username),
            new(JwtRegisteredClaimNames.UniqueName, displayName),
            new("role", role),
        };

        if (employeeId.HasValue)
            claims.Add(new Claim("EmployeeId", employeeId.Value.ToString()));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expiresInHours),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static IConfiguration BuildFallbackConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = "PerformanceReviewSystem-SuperSecret-Key-2026!",
                ["JwtSettings:Issuer"] = "PerformanceReviewApi",
                ["JwtSettings:Audience"] = "PerformanceReviewClient",
                ["JwtSettings:ExpiresInHours"] = "8",
            })
            .Build();
}

public record LoginRequest(string Username, string Password);
