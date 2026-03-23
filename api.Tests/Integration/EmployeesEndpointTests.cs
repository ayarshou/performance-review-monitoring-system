using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace PerformanceReviewApi.Tests.Integration;

public class EmployeesEndpointTests : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client;

    public EmployeesEndpointTests(TestWebAppFactory factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task GetAll_Returns200WithSeeded13Employees()
    {
        var response = await _client.GetAsync("/api/employees");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var employees = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        // >= 13 because other tests in this class may have added employees to the shared DB
        Assert.True(employees!.Length >= 13, $"Expected at least 13 employees, got {employees!.Length}");
    }

    [Fact]
    public async Task GetAll_EmployeesDoNotExposePasswordHash()
    {
        var raw = await _client.GetStringAsync("/api/employees");
        Assert.DoesNotContain("passwordHash", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetById_SeededEmployee_Returns200()
    {
        // Get the list first to find a valid id
        var list = await _client.GetFromJsonAsync<JsonElement[]>("/api/employees");
        var firstId = list![0].GetProperty("id").GetInt32();

        var response = await _client.GetAsync($"/api/employees/{firstId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetById_NonExistingId_Returns404()
    {
        var response = await _client.GetAsync("/api/employees/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateEmployee_ValidPayload_Returns201AndIncrementsCount()
    {
        var payload = new
        {
            name = "New Employee", email = "new@company.com",
            position = "Tester", hireDate = DateTime.UtcNow,
        };

        var response = await _client.PostAsJsonAsync("/api/employees", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var allEmployees = await _client.GetFromJsonAsync<JsonElement[]>("/api/employees");
        Assert.Equal(14, allEmployees!.Length); // 13 seeded + 1 new
    }
}
