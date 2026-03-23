using PerformanceReviewApi.Controllers;
using PerformanceReviewApi.Data;
using PerformanceReviewApi.Helpers;

namespace PerformanceReviewApi.Tests.Controllers;

public class AuthControllerTests
{
    private static Employee SeedEmployee(AppDbContext db, string username, string password)
    {
        var emp = new Employee
        {
            Name = "Test User", Email = $"{username}@test.com",
            Position = "Engineer", HireDate = DateTime.UtcNow.AddYears(-1),
            Username = username, PasswordHash = PasswordHelper.Hash(password),
        };
        db.Employees.Add(emp);
        db.SaveChanges();
        return emp;
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithEmployeeData()
    {
        using var db = TestDbContextFactory.Create();
        var emp = SeedEmployee(db, "alice", "Review2026!");
        var controller = new AuthController(db);

        var result = await controller.Login(new LoginRequest("alice", "Review2026!"));

        var ok = Assert.IsType<OkObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(ok.Value);
        Assert.Contains("\"name\"", json);
        Assert.Contains("Test User", json); // SeedEmployee sets Name = "Test User"
    }

    [Fact]
    public async Task Login_PasswordHashNotIncludedInResponse()
    {
        using var db = TestDbContextFactory.Create();
        SeedEmployee(db, "bob", "Pass123!");
        var controller = new AuthController(db);

        var result = await controller.Login(new LoginRequest("bob", "Pass123!"));

        var ok = Assert.IsType<OkObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(ok.Value);
        Assert.DoesNotContain("passwordHash", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        using var db = TestDbContextFactory.Create();
        SeedEmployee(db, "carol", "CorrectPass!");
        var controller = new AuthController(db);

        var result = await controller.Login(new LoginRequest("carol", "WrongPass!"));

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_UnknownUsername_ReturnsUnauthorized()
    {
        using var db = TestDbContextFactory.Create();
        var controller = new AuthController(db);

        var result = await controller.Login(new LoginRequest("nobody", "pass"));

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_EmptyUsername_ReturnsBadRequest()
    {
        using var db = TestDbContextFactory.Create();
        var controller = new AuthController(db);

        var result = await controller.Login(new LoginRequest("", "pass"));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Login_EmptyPassword_ReturnsBadRequest()
    {
        using var db = TestDbContextFactory.Create();
        var controller = new AuthController(db);

        var result = await controller.Login(new LoginRequest("user", ""));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Login_UsernameMatchIsCaseInsensitive()
    {
        using var db = TestDbContextFactory.Create();
        SeedEmployee(db, "david", "Pass123!");
        var controller = new AuthController(db);

        // The controller trims and lowercases the input; stored username is already lowercase
        var result = await controller.Login(new LoginRequest("DAVID", "Pass123!"));

        Assert.IsType<OkObjectResult>(result);
    }
}
