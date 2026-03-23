using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PerformanceReviewApi.Data;

namespace PerformanceReviewApi.Tests.Integration;

/// <summary>
/// Spins up the full ASP.NET Core pipeline with an isolated InMemory database per factory instance.
/// The DbSeeder runs on startup, giving every test class a freshly-seeded dataset.
/// </summary>
public class TestWebAppFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"TestDb_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the SQL Server DbContext so that startup doesn't need a real connection string
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));
        });

        builder.UseEnvironment("Testing");
    }
}
