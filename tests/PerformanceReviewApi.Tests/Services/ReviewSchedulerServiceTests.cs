using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PerformanceReviewApi.Data;
using PerformanceReviewApi.Models;
using PerformanceReviewApi.Services;

namespace PerformanceReviewApi.Tests.Services;

/// <summary>
/// Minimal <see cref="TimeProvider"/> implementation that returns a fixed UTC instant.
/// Used to make date-sensitive logic deterministic in tests.
/// </summary>
internal sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _fixedUtcNow;
    public FixedTimeProvider(DateTimeOffset fixedUtcNow) => _fixedUtcNow = fixedUtcNow;
    public override DateTimeOffset GetUtcNow() => _fixedUtcNow;
}

/// <summary>
/// Unit tests for <see cref="ReviewSchedulerService"/>.
///
/// Uses the EF Core InMemory provider to avoid a real database and Moq to assert
/// on email delivery without a real SMTP server.
/// All tests operate against a fixed date (2026-03-15) to eliminate
/// month-boundary flakiness.
/// </summary>
public class ReviewSchedulerServiceTests : IDisposable
{
    // ── Fixed clock: mid-March 2026 ───────────────────────────────────────────
    private static readonly DateTimeOffset FixedNow = new DateTimeOffset(2026, 3, 15, 12, 0, 0, TimeSpan.Zero);
    private static readonly FixedTimeProvider Clock = new(FixedNow);

    // Convenience date helpers aligned to the fixed clock
    private static DateTime ThisMonthDeadline => new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc);
    private static DateTime NextMonthDeadline => new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc);
    private static DateTime WithinThreeDays    => new DateTime(2026, 3, 17, 0, 0, 0, DateTimeKind.Utc); // today + 2
    private static DateTime BeyondThreeDays   => new DateTime(2026, 3, 25, 0, 0, 0, DateTimeKind.Utc); // today + 10

    // ── Shared mocks ──────────────────────────────────────────────────────────
    private readonly Mock<IEmailService> _emailMock = new();
    private readonly Mock<ILogger<ReviewSchedulerService>> _loggerMock = new();
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock = new();

    private AppDbContext CreateInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new AppDbContext(options);
    }

    private ReviewSchedulerService CreateService() =>
        new ReviewSchedulerService(_scopeFactoryMock.Object, _emailMock.Object, _loggerMock.Object, Clock);

    public void Dispose() { /* in-memory DBs are isolated per test via unique names */ }

    // ── Helper: wire scope factory so RunScheduledChecksAsync uses our DbContext ──
    private void SetupScopeFactory(AppDbContext db)
    {
        var scopeMock = new Mock<IServiceScope>();
        var providerMock = new Mock<IServiceProvider>();

        providerMock.Setup(p => p.GetService(typeof(AppDbContext))).Returns(db);
        scopeMock.Setup(s => s.ServiceProvider).Returns(providerMock.Object);
        _scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // SendMonthlyReviewNotificationsAsync
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SendMonthlyReviewNotifications_SendsEmail_OnlyToPendingEmployees()
    {
        // Arrange ────────────────────────────────────────────────────────────
        var db = CreateInMemoryDbContext(nameof(SendMonthlyReviewNotifications_SendsEmail_OnlyToPendingEmployees));

        var alice = new Employee { Id = 1, Name = "Alice", Email = "alice@example.com", Position = "Dev", HireDate = FixedNow.UtcDateTime.AddYears(-1) };
        var bob   = new Employee { Id = 2, Name = "Bob",   Email = "bob@example.com",   Position = "Dev", HireDate = FixedNow.UtcDateTime.AddYears(-1) };

        db.Employees.AddRange(alice, bob);
        db.ReviewSessions.AddRange(
            new ReviewSession { EmployeeId = 1, Status = ReviewStatus.Pending,   ScheduledDate = ThisMonthDeadline.AddDays(-5), Deadline = ThisMonthDeadline },
            new ReviewSession { EmployeeId = 2, Status = ReviewStatus.Completed, ScheduledDate = ThisMonthDeadline.AddDays(-5), Deadline = ThisMonthDeadline }
        );
        await db.SaveChangesAsync();

        var service = CreateService();

        // Act ────────────────────────────────────────────────────────────────
        await service.SendMonthlyReviewNotificationsAsync(db);

        // Assert ─────────────────────────────────────────────────────────────
        // Alice (Pending) receives exactly one email; Bob (Completed) receives none.
        _emailMock.Verify(
            e => e.SendEmailAsync("alice@example.com", "Alice", It.IsAny<string>(), It.IsAny<string>()),
            Times.Once,
            "A pending employee must receive a reminder email.");

        _emailMock.Verify(
            e => e.SendEmailAsync("bob@example.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never,
            "A completed employee must not receive any email.");
    }

    [Fact]
    public async Task SendMonthlyReviewNotifications_SendsNoEmail_WhenNoSessionsDueThisMonth()
    {
        // Arrange ────────────────────────────────────────────────────────────
        var db = CreateInMemoryDbContext(nameof(SendMonthlyReviewNotifications_SendsNoEmail_WhenNoSessionsDueThisMonth));

        var employee = new Employee { Id = 1, Name = "Carol", Email = "carol@example.com", Position = "QA", HireDate = FixedNow.UtcDateTime.AddYears(-1) };
        db.Employees.Add(employee);

        // Deadline is next month ─ must NOT trigger this month's notification pass
        db.ReviewSessions.Add(new ReviewSession
        {
            EmployeeId = 1,
            Status = ReviewStatus.Pending,
            ScheduledDate = NextMonthDeadline.AddDays(-5),
            Deadline = NextMonthDeadline
        });
        await db.SaveChangesAsync();

        var service = CreateService();

        // Act ────────────────────────────────────────────────────────────────
        await service.SendMonthlyReviewNotificationsAsync(db);

        // Assert ─────────────────────────────────────────────────────────────
        _emailMock.Verify(
            e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task SendMonthlyReviewNotifications_SendsEmail_ToEveryPendingEmployee()
    {
        // Arrange ────────────────────────────────────────────────────────────
        var db = CreateInMemoryDbContext(nameof(SendMonthlyReviewNotifications_SendsEmail_ToEveryPendingEmployee));

        for (int i = 1; i <= 3; i++)
        {
            db.Employees.Add(new Employee { Id = i, Name = $"Employee{i}", Email = $"emp{i}@example.com", Position = "Dev", HireDate = FixedNow.UtcDateTime.AddYears(-1) });
            db.ReviewSessions.Add(new ReviewSession { EmployeeId = i, Status = ReviewStatus.Pending, ScheduledDate = ThisMonthDeadline.AddDays(-5), Deadline = ThisMonthDeadline });
        }
        await db.SaveChangesAsync();

        var service = CreateService();

        // Act ────────────────────────────────────────────────────────────────
        await service.SendMonthlyReviewNotificationsAsync(db);

        // Assert ─────────────────────────────────────────────────────────────
        _emailMock.Verify(
            e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Exactly(3),
            "Each of the three pending employees must receive one email.");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // SendManagerSummariesAsync
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SendManagerSummaries_SendsSummary_WhenOverdueReviewsExistInTeam()
    {
        // Arrange ────────────────────────────────────────────────────────────
        var db = CreateInMemoryDbContext(nameof(SendManagerSummaries_SendsSummary_WhenOverdueReviewsExistInTeam));

        var manager = new Employee { Id = 1, Name = "Manager Mike", Email = "mike@example.com", Position = "Manager", HireDate = FixedNow.UtcDateTime.AddYears(-3) };
        var report  = new Employee { Id = 2, Name = "Report Rita",  Email = "rita@example.com", Position = "Dev",     HireDate = FixedNow.UtcDateTime.AddYears(-1), ManagerId = 1 };

        db.Employees.AddRange(manager, report);
        db.ReviewSessions.Add(new ReviewSession
        {
            EmployeeId = 2,
            Status = ReviewStatus.Pending,
            ScheduledDate = WithinThreeDays.AddDays(-10),
            Deadline = WithinThreeDays
        });
        await db.SaveChangesAsync();

        var service = CreateService();

        // Act ────────────────────────────────────────────────────────────────
        await service.SendManagerSummariesAsync(db);

        // Assert ─────────────────────────────────────────────────────────────
        _emailMock.Verify(
            e => e.SendEmailAsync("mike@example.com", "Manager Mike", It.IsAny<string>(), It.IsAny<string>()),
            Times.Once,
            "The manager must receive a summary when a team member has an overdue review.");
    }

    [Fact]
    public async Task SendManagerSummaries_DoesNotSendSummary_WhenNoOverdueReviewsExist()
    {
        // Arrange ────────────────────────────────────────────────────────────
        var db = CreateInMemoryDbContext(nameof(SendManagerSummaries_DoesNotSendSummary_WhenNoOverdueReviewsExist));

        var manager = new Employee { Id = 1, Name = "Manager Sam", Email = "sam@example.com", Position = "Manager", HireDate = FixedNow.UtcDateTime.AddYears(-3) };
        var report  = new Employee { Id = 2, Name = "Report Dan",  Email = "dan@example.com", Position = "Dev",     HireDate = FixedNow.UtcDateTime.AddYears(-1), ManagerId = 1 };

        db.Employees.AddRange(manager, report);

        // Deadline well beyond the 3-day window
        db.ReviewSessions.Add(new ReviewSession
        {
            EmployeeId = 2,
            Status = ReviewStatus.Pending,
            ScheduledDate = BeyondThreeDays.AddDays(-10),
            Deadline = BeyondThreeDays
        });
        await db.SaveChangesAsync();

        var service = CreateService();

        // Act ────────────────────────────────────────────────────────────────
        await service.SendManagerSummariesAsync(db);

        // Assert ─────────────────────────────────────────────────────────────
        _emailMock.Verify(
            e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never,
            "No summary must be sent when no reviews are approaching the 3-day deadline.");
    }

    [Fact]
    public async Task SendManagerSummaries_DoesNotSendSummary_WhenAllReviewsAreCompleted()
    {
        // Arrange ────────────────────────────────────────────────────────────
        var db = CreateInMemoryDbContext(nameof(SendManagerSummaries_DoesNotSendSummary_WhenAllReviewsAreCompleted));

        var manager = new Employee { Id = 1, Name = "Manager Eve",  Email = "eve@example.com",   Position = "Manager", HireDate = FixedNow.UtcDateTime.AddYears(-3) };
        var report  = new Employee { Id = 2, Name = "Report Frank", Email = "frank@example.com", Position = "Dev",     HireDate = FixedNow.UtcDateTime.AddYears(-1), ManagerId = 1 };

        db.Employees.AddRange(manager, report);

        // Deadline within 3 days but status is Completed
        db.ReviewSessions.Add(new ReviewSession
        {
            EmployeeId = 2,
            Status = ReviewStatus.Completed,
            ScheduledDate = WithinThreeDays.AddDays(-10),
            Deadline = WithinThreeDays
        });
        await db.SaveChangesAsync();

        var service = CreateService();

        // Act ────────────────────────────────────────────────────────────────
        await service.SendManagerSummariesAsync(db);

        // Assert ─────────────────────────────────────────────────────────────
        _emailMock.Verify(
            e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never,
            "Completed reviews must not trigger a manager summary.");
    }

    [Fact]
    public async Task SendManagerSummaries_SendsOneSummaryPerManager_ForMultipleReports()
    {
        // Arrange ────────────────────────────────────────────────────────────
        var db = CreateInMemoryDbContext(nameof(SendManagerSummaries_SendsOneSummaryPerManager_ForMultipleReports));

        var manager = new Employee { Id = 1, Name = "Manager Leo", Email = "leo@example.com", Position = "Manager", HireDate = FixedNow.UtcDateTime.AddYears(-3) };
        var r1      = new Employee { Id = 2, Name = "Report One",  Email = "one@example.com",  Position = "Dev", HireDate = FixedNow.UtcDateTime.AddYears(-1), ManagerId = 1 };
        var r2      = new Employee { Id = 3, Name = "Report Two",  Email = "two@example.com",  Position = "Dev", HireDate = FixedNow.UtcDateTime.AddYears(-1), ManagerId = 1 };

        db.Employees.AddRange(manager, r1, r2);
        db.ReviewSessions.AddRange(
            new ReviewSession { EmployeeId = 2, Status = ReviewStatus.Pending, ScheduledDate = WithinThreeDays.AddDays(-10), Deadline = WithinThreeDays },
            new ReviewSession { EmployeeId = 3, Status = ReviewStatus.Pending, ScheduledDate = WithinThreeDays.AddDays(-10), Deadline = WithinThreeDays }
        );
        await db.SaveChangesAsync();

        var service = CreateService();

        // Act ────────────────────────────────────────────────────────────────
        await service.SendManagerSummariesAsync(db);

        // Assert ─────────────────────────────────────────────────────────────
        // Manager receives exactly ONE consolidated email even though two reports are overdue
        _emailMock.Verify(
            e => e.SendEmailAsync("leo@example.com", "Manager Leo", It.IsAny<string>(), It.IsAny<string>()),
            Times.Once,
            "Each manager must receive exactly one summary regardless of how many reports are overdue.");
    }

    [Fact]
    public async Task SendManagerSummaries_DoesNotSendToManager_WhenEmployeeHasNoManager()
    {
        // Arrange ────────────────────────────────────────────────────────────
        var db = CreateInMemoryDbContext(nameof(SendManagerSummaries_DoesNotSendToManager_WhenEmployeeHasNoManager));

        // Top-level employee (no manager)
        var employee = new Employee { Id = 1, Name = "Solo Sue", Email = "sue@example.com", Position = "CTO", HireDate = FixedNow.UtcDateTime.AddYears(-5) };
        db.Employees.Add(employee);

        db.ReviewSessions.Add(new ReviewSession
        {
            EmployeeId = 1,
            Status = ReviewStatus.Pending,
            ScheduledDate = WithinThreeDays.AddDays(-10),
            Deadline = WithinThreeDays
        });
        await db.SaveChangesAsync();

        var service = CreateService();

        // Act ────────────────────────────────────────────────────────────────
        await service.SendManagerSummariesAsync(db);

        // Assert ─────────────────────────────────────────────────────────────
        _emailMock.Verify(
            e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never,
            "No email should be sent when the overdue employee has no manager.");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Dependency-Injection / constructor null guards
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenScopeFactoryIsNull()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new ReviewSchedulerService(null!, _emailMock.Object, _loggerMock.Object));

        Assert.Equal("scopeFactory", ex.ParamName);
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenEmailServiceIsNull()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new ReviewSchedulerService(_scopeFactoryMock.Object, null!, _loggerMock.Object));

        Assert.Equal("emailService", ex.ParamName);
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new ReviewSchedulerService(_scopeFactoryMock.Object, _emailMock.Object, null!));

        Assert.Equal("logger", ex.ParamName);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // RunScheduledChecksAsync (integration of both passes via scope)
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task RunScheduledChecks_UsesNewScope_ForDbContext()
    {
        // Arrange ────────────────────────────────────────────────────────────
        var db = CreateInMemoryDbContext(nameof(RunScheduledChecks_UsesNewScope_ForDbContext));
        SetupScopeFactory(db);

        var service = CreateService();

        // Act ─ should complete without exception even with an empty database
        await service.RunScheduledChecksAsync();

        // Assert ─────────────────────────────────────────────────────────────
        _scopeFactoryMock.Verify(f => f.CreateScope(), Times.Once,
            "A new DI scope must be created for each scheduled run.");
    }
}
