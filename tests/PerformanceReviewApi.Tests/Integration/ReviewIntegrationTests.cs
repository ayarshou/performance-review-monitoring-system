using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PerformanceReviewApi.Data;
using PerformanceReviewApi.DTOs;
using PerformanceReviewApi.Helpers;
using PerformanceReviewApi.Models;
using PerformanceReviewApi.Services;

namespace PerformanceReviewApi.Tests.Integration;

/// <summary>
/// Integration tests using WebApplicationFactory that:
///   1. Mimic a user login (POST /api/auth/login).
///   2. Submit a performance review (POST /api/reviews/{id}/submit).
///   3. Verify the database record is updated correctly.
/// </summary>
public class ReviewIntegrationTests : IClassFixture<ReviewApiFactory>
{
    private readonly ReviewApiFactory _factory;

    public ReviewIntegrationTests(ReviewApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Username = "manager1", Password = "Pass@123" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var token = body.GetProperty("token").GetString();
        Assert.False(string.IsNullOrEmpty(token));
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Username = "manager1", Password = "WrongPassword" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SubmitReview_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        int reviewId = _factory.SeedData.ReviewSessionId;

        var response = await client.PostAsJsonAsync(
            $"/api/reviews/{reviewId}/submit",
            new SubmitReviewRequest { Notes = "Good work." });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SubmitReview_AsAuthenticatedUser_UpdatesDatabaseRecord()
    {
        var client = _factory.CreateClient();

        // Step 1: Login as the employee user
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Username = "employee1", Password = "Pass@123" });
        loginResponse.EnsureSuccessStatusCode();

        var tokenBody = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = tokenBody.GetProperty("token").GetString()!;

        // Step 2: Submit the review
        int reviewId = _factory.SeedData.ReviewSessionId;
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var submitResponse = await client.PostAsJsonAsync(
            $"/api/reviews/{reviewId}/submit",
            new SubmitReviewRequest { Notes = "Completed successfully." });

        Assert.Equal(HttpStatusCode.NoContent, submitResponse.StatusCode);

        // Step 3: Verify the database record is updated
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var session = await db.ReviewSessions.FindAsync(reviewId);

        Assert.NotNull(session);
        Assert.Equal(ReviewStatus.Completed, session!.Status);
        Assert.Equal("Completed successfully.", session.Notes);
    }

    [Fact]
    public async Task SubmitReview_WithNotesExceedingLimit_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Username = "employee1", Password = "Pass@123" });
        loginResponse.EnsureSuccessStatusCode();

        var tokenBody = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = tokenBody.GetProperty("token").GetString()!;
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        int reviewId = _factory.SeedData.ReviewSessionId;
        var tooLongNotes = new string('x', 2001);

        var response = await client.PostAsJsonAsync(
            $"/api/reviews/{reviewId}/submit",
            new SubmitReviewRequest { Notes = tooLongNotes });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetTeamStatus_AsManager_ReturnsSubordinatesWithReviewStatus()
    {
        var client = _factory.CreateClient();

        // Login as manager
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Username = "manager1", Password = "Pass@123" });
        loginResponse.EnsureSuccessStatusCode();

        var tokenBody = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = tokenBody.GetProperty("token").GetString()!;
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/reviews/team-status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(result.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task GetTeamStatus_AsEmployee_ReturnsForbidden()
    {
        var client = _factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Username = "employee1", Password = "Pass@123" });
        loginResponse.EnsureSuccessStatusCode();

        var tokenBody = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = tokenBody.GetProperty("token").GetString()!;
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/reviews/team-status");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

/// <summary>
/// Holds the IDs of seeded test data so tests can reference them.
/// </summary>
public class SeedData
{
    public int ManagerEmployeeId { get; set; }
    public int EmployeeId { get; set; }
    public int ReviewSessionId { get; set; }
}

/// <summary>
/// Custom WebApplicationFactory that replaces SQL Server with an in-memory database
/// and stubs out the email service so no real SMTP is needed.
/// </summary>
public class ReviewApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public SeedData SeedData { get; } = new SeedData();
    private readonly string _dbName = "TestDb_" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the real SQL Server DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Remove hosted background service (uses real email)
            var schedulerDescriptor = services.SingleOrDefault(
                d => d.ImplementationType == typeof(ReviewSchedulerService));
            if (schedulerDescriptor != null)
                services.Remove(schedulerDescriptor);

            // Remove the real singleton email service (validates SMTP config at startup)
            var emailDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IEmailService));
            if (emailDescriptor != null)
                services.Remove(emailDescriptor);

            // Replace with a no-op email service
            services.AddSingleton<IEmailService, NoOpEmailService>();

            // Use a unique in-memory database per test factory instance
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));
        });
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        // Seed after the server is built so we use the actual app's service provider
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
        await SeedTestDataAsync(db);
    }

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;

    private async Task SeedTestDataAsync(AppDbContext db)
    {
        // Manager employee
        var managerEmployee = new Employee
        {
            Name = "Alice Manager",
            Email = "alice@example.com",
            Position = "Engineering Manager",
            HireDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        db.Employees.Add(managerEmployee);
        await db.SaveChangesAsync();
        SeedData.ManagerEmployeeId = managerEmployee.Id;

        // Subordinate employee
        var employee = new Employee
        {
            Name = "Bob Employee",
            Email = "bob@example.com",
            Position = "Software Engineer",
            HireDate = new DateTime(2022, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            ManagerId = managerEmployee.Id
        };
        db.Employees.Add(employee);
        await db.SaveChangesAsync();
        SeedData.EmployeeId = employee.Id;

        // Review session for the employee
        var session = new ReviewSession
        {
            EmployeeId = employee.Id,
            Status = ReviewStatus.Pending,
            ScheduledDate = DateTime.UtcNow,
            Deadline = DateTime.UtcNow.AddDays(30)
        };
        db.ReviewSessions.Add(session);
        await db.SaveChangesAsync();
        SeedData.ReviewSessionId = session.Id;

        // Manager user (linked to manager employee)
        db.Users.Add(new User
        {
            Username = "manager1",
            PasswordHash = PasswordHashHelper.Hash("Pass@123"),
            Role = "Manager",
            EmployeeId = managerEmployee.Id
        });

        // Employee user (linked to subordinate employee)
        db.Users.Add(new User
        {
            Username = "employee1",
            PasswordHash = PasswordHashHelper.Hash("Pass@123"),
            Role = "Employee",
            EmployeeId = employee.Id
        });

        await db.SaveChangesAsync();
    }
}

/// <summary>
/// No-op implementation of IEmailService for use in integration tests.
/// </summary>
public class NoOpEmailService : IEmailService
{
    public Task SendEmailAsync(string toAddress, string toName, string subject, string body)
        => Task.CompletedTask;
}
