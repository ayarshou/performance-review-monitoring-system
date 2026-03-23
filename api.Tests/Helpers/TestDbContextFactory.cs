using Microsoft.EntityFrameworkCore;
using PerformanceReviewApi.Data;

namespace PerformanceReviewApi.Tests.Helpers;

/// <summary>Creates a fresh EF Core InMemory context per test — each call gets its own isolated database.</summary>
public static class TestDbContextFactory
{
    public static AppDbContext Create(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
