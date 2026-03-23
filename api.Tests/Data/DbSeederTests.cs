using PerformanceReviewApi.Data;

namespace PerformanceReviewApi.Tests.Data;

public class DbSeederTests
{
    [Fact]
    public void Seed_EmptyDatabase_Creates13Employees()
    {
        using var db = TestDbContextFactory.Create();
        DbSeeder.Seed(db);
        Assert.Equal(13, db.Employees.Count());
    }

    [Fact]
    public void Seed_EmptyDatabase_Creates12ReviewSessions()
    {
        using var db = TestDbContextFactory.Create();
        DbSeeder.Seed(db);
        Assert.Equal(12, db.ReviewSessions.Count());
    }

    [Fact]
    public void Seed_EmptyDatabase_Creates10PendingAnd2CompletedSessions()
    {
        using var db = TestDbContextFactory.Create();
        DbSeeder.Seed(db);
        Assert.Equal(10, db.ReviewSessions.Count(rs => rs.Status == ReviewStatus.Pending));
        Assert.Equal(2,  db.ReviewSessions.Count(rs => rs.Status == ReviewStatus.Completed));
    }

    [Fact]
    public void Seed_CalledTwice_IsIdempotent()
    {
        using var db = TestDbContextFactory.Create();
        DbSeeder.Seed(db);
        DbSeeder.Seed(db); // second call must be a no-op
        Assert.Equal(13, db.Employees.Count());
        Assert.Equal(12, db.ReviewSessions.Count());
    }

    [Fact]
    public void Seed_AllEmployeesHaveHashedPasswords()
    {
        using var db = TestDbContextFactory.Create();
        DbSeeder.Seed(db);
        Assert.All(db.Employees, emp =>
        {
            Assert.NotNull(emp.PasswordHash);
            // Hash must be in salt:hash format — never plain-text
            Assert.Contains(":", emp.PasswordHash);
            Assert.NotEqual("Review2026!", emp.PasswordHash);
        });
    }

    [Fact]
    public void Seed_AllEmployeesHaveUsernames()
    {
        using var db = TestDbContextFactory.Create();
        DbSeeder.Seed(db);
        Assert.All(db.Employees, emp => Assert.NotNull(emp.Username));
    }

    [Fact]
    public void Seed_ManagersHaveSubordinates()
    {
        using var db = TestDbContextFactory.Create();
        DbSeeder.Seed(db);
        var alice = db.Employees.First(e => e.Username == "alice");
        Assert.Equal(4, db.Employees.Count(e => e.ManagerId == alice.Id));
    }

    [Fact]
    public void Seed_SeededPasswordsAreVerifiable()
    {
        using var db = TestDbContextFactory.Create();
        DbSeeder.Seed(db);
        var alice = db.Employees.First(e => e.Username == "alice");
        Assert.True(PerformanceReviewApi.Helpers.PasswordHelper.Verify("Review2026!", alice.PasswordHash!));
    }
}
