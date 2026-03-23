using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace PerformanceReviewApi.Tests.Integration;

public class AuthEndpointTests : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointTests(TestWebAppFactory factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithEmployeeInfo()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { username = "alice", password = "Review2026!" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Alice Johnson", body.GetProperty("name").GetString());
        Assert.Equal("alice.johnson@company.com", body.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Login_ValidCredentials_ResponseDoesNotContainPasswordHash()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { username = "bob", password = "Review2026!" });

        var raw = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("passwordHash", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { username = "alice", password = "WrongPassword!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownUser_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { username = "nobody", password = "anything" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_EmptyBody_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { username = "", password = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
