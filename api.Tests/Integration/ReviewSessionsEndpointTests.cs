using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace PerformanceReviewApi.Tests.Integration;

public class ReviewSessionsEndpointTests : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client;

    public ReviewSessionsEndpointTests(TestWebAppFactory factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task GetAll_Returns200WithSeeded12Sessions()
    {
        var response = await _client.GetAsync("/api/reviewsessions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var sessions = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        Assert.Equal(12, sessions!.Length);
    }

    [Fact]
    public async Task GetAll_HasBothPendingAndCompletedStatuses()
    {
        var sessions = await _client.GetFromJsonAsync<JsonElement[]>("/api/reviewsessions");
        var statuses = sessions!.Select(s => s.GetProperty("status").GetString()).ToHashSet();
        Assert.Contains("Pending",   statuses);
        Assert.Contains("Completed", statuses);
    }

    [Fact]
    public async Task GetById_SeededSession_Returns200()
    {
        var sessions = await _client.GetFromJsonAsync<JsonElement[]>("/api/reviewsessions");
        var firstId  = sessions![0].GetProperty("id").GetInt32();

        var response = await _client.GetAsync($"/api/reviewsessions/{firstId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetById_NonExistingSession_Returns404()
    {
        var response = await _client.GetAsync("/api/reviewsessions/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetByEmployee_Returns200WithSessionsForThatEmployee()
    {
        // Employee IDs 4-13 are the non-manager employees with pending reviews
        var sessions = await _client.GetFromJsonAsync<JsonElement[]>("/api/reviewsessions");
        var empId    = sessions![0].GetProperty("employeeId").GetInt32();

        var response = await _client.GetAsync($"/api/reviewsessions/employee/{empId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var filtered = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        Assert.All(filtered!, s => Assert.Equal(empId, s.GetProperty("employeeId").GetInt32()));
    }

    [Fact]
    public async Task CreateSession_ValidPayload_Returns201()
    {
        // Get a valid employee id from the seeded data
        var employees = await _client.GetFromJsonAsync<JsonElement[]>("/api/employees");
        var empId = employees![0].GetProperty("id").GetInt32();

        var payload = new
        {
            employeeId    = empId,
            status        = "Pending",
            scheduledDate = DateTime.UtcNow.AddDays(14),
            deadline      = DateTime.UtcNow.AddDays(45),
        };

        var response = await _client.PostAsJsonAsync("/api/reviewsessions", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
