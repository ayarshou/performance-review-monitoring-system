using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PerformanceReviewApi.Models;
using PerformanceReviewApi.Repositories;
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
/// Uses Moq to mock <see cref="IReviewSessionRepository"/> and <see cref="IEmailService"/>
/// so no real database or SMTP server is required.
/// All tests operate against a fixed date (2026-03-15) to eliminate month-boundary flakiness.
/// </summary>
public class ReviewSchedulerServiceTests
{
    // ── Fixed clock: mid-March 2026 ───────────────────────────────────────────
    private static readonly DateTimeOffset FixedNow = new DateTimeOffset(2026, 3, 15, 12, 0, 0, TimeSpan.Zero);
    private static readonly FixedTimeProvider Clock = new(FixedNow);

    // Convenience date helpers aligned to the fixed clock
    private static DateTime ThisMonthDeadline => new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc);
    private static DateTime NextMonthDeadline => new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc);
    private static DateTime WithinThreeDays   => new DateTime(2026, 3, 17, 0, 0, 0, DateTimeKind.Utc); // today + 2
    private static DateTime BeyondThreeDays   => new DateTime(2026, 3, 25, 0, 0, 0, DateTimeKind.Utc); // today + 10

    // ── Shared mocks ──────────────────────────────────────────────────────────
    private readonly Mock<IEmailService> _emailMock = new();
    private readonly Mock<ILogger<ReviewSchedulerService>> _loggerMock = new();
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock = new();
    private readonly Mock<IReviewSessionRepository> _reviewRepoMock = new();

    private ReviewSchedulerService CreateService() =>
        new ReviewSchedulerService(_scopeFactoryMock.Object, _emailMock.Object, _loggerMock.Object, Clock);

    // ── Helper: wire scope factory to resolve the mocked repository ──────────
    private void SetupScopeFactory()
    {
        var scopeMock = new Mock<IServiceScope>();
        var providerMock = new Mock<IServiceProvider>();

        providerMock
            .Setup(p => p.GetService(typeof(IReviewSessionRepository)))
            .Returns(_reviewRepoMock.Object);
        scopeMock.Setup(s => s.ServiceProvider).Returns(providerMock.Object);
        _scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);
    }

    // ── Helper: build a ReviewSession with an eagerly-loaded Employee ─────────
    private static ReviewSession MakeSession(Employee employee, DateTime deadline, ReviewStatus status = ReviewStatus.Pending) =>
        new ReviewSession
        {
            EmployeeId = employee.Id,
            Employee = employee,
            Status = status,
            ScheduledDate = deadline.AddDays(-10),
            Deadline = deadline
        };

    // ═════════════════════════════════════════════════════════════════════════
    // SendMonthlyReviewNotificationsAsync
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SendMonthlyReviewNotifications_SendsEmail_OnlyToPendingEmployees()
    {
        // Arrange ────────────────────────────────────────────────────────────
        var alice = new Employee { Id = 1, Name = "Alice", Email = "alice@example.com", Position = "Dev", HireDate = FixedNow.UtcDateTime.AddYears(-1) };

        // The repository already filters by Pending; only Alice's session is returned.
        _reviewRepoMock
            .Setup(r => r.GetPendingDueInRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new[] { MakeSession(alice, ThisMonthDeadline) });

        var service = CreateService();

        // Act ────────────────────────────────────────────────────────────────
        await service.SendMonthlyReviewNotificationsAsync(_reviewRepoMock.Object);

        // Assert ─────────────────────────────────────────────────────────────
        _emailMock.Verify(
            e => e.SendEmailAsync("alice@example.com", "Alice", It.IsAny<string>(), It.IsAny<string>()),
            Times.Once,
            "An email must be sent for each session returned by the repository.");

        // Verify no extra emails were sent
        _emailMock.Verify(
            e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Once,
            "Exactly one email must be sent when the repository returns one session.");
    }

    [Fact]
    public async Task SendMonthlyReviewNotifications_SendsNoEmail_WhenRepositoryReturnsEmpty()
    {
        // Arrange ────────────────────────────────────────────────────────────
        _reviewRepoMock
            .Setup(r => r.GetPendingDueInRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(Enumerable.Empty<ReviewSession>());

        var service = CreateService();

        // Act ────────────────────────────────────────────────────────────────
        await service.SendMonthlyReviewNotificationsAsync(_reviewRepoMock.Object);

        // Assert ─────────────────────────────────────────────────────────────
        _emailMock.Verify(
            e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never,
            "No email must be sent when there are no pending sessions this month.");
    }

    [Fact]
    public async Task SendMonthlyReviewNotifications_PassesCorrectDateRange_ToRepository()
    {
        // Arrange ────────────────────────────────────────────────────────────
        // Fixed clock is 2026-03-15 → expected range: [2026-03-01, 2026-04-01)
        var expectedFrom = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var expectedTo   = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);

        _reviewRepoMock
            .Setup(r => r.GetPendingDueInRangeAsync(expectedFrom, expectedTo))
            .ReturnsAsync(Enumerable.Empty<ReviewSession>())
            .Verifiable("Repository must be queried with the correct month boundaries.");

        var service = CreateService();

        // Act ────────────────────────────────────────────────────────────────
        await service.SendMonthlyReviewNotificationsAsync(_reviewRepoMock.Object);

        // Assert ─────────────────────────────────────────────────────────────
        _reviewRepoMock.Verify();
    }

    [Fact]
    public async Task SendMonthlyReviewNotifications_SendsEmail_ToEveryPendingEmployee()
    {
        // Arrange ────────────────────────────────────────────────────────────
        var sessions = Enumerable.Range(1, 3)
            .Select(i => MakeSession(
                new Employee { Id = i, Name = $"Emp{i}", Email = $"emp{i}@example.com", Position = "Dev", HireDate = FixedNow.UtcDateTime.AddYears(-1) },
                ThisMonthDeadline))
            .ToArray();

        _reviewRepoMock
            .Setup(r => r.GetPendingDueInRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(sessions);

        var service = CreateService();

        // Act ────────────────────────────────────────────────────────────────
        await service.SendMonthlyReviewNotificationsAsync(_reviewRepoMock.Object);

        // Assert ─────────────────────────────────────────────────────────────
        _emailMock.Verify(
            e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Exactly(3),
            "Each of the three pending sessions must trigger one email.");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // SendManagerSummariesAsync
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SendManagerSummaries_SendsSummary_WhenOverdueReviewsExistInTeam()
    {
        // Arrange ────────────────────────────────────────────────────────────
        var manager = new Employee { Id = 1, Name = "Manager Mike", Email = "mike@example.com", Position = "Manager", HireDate = FixedNow.UtcDateTime.AddYears(-3) };
        var report  = new Employee { Id = 2, Name = "Report Rita",  Email = "rita@example.com", Position = "Dev",     HireDate = FixedNow.UtcDateTime.AddYears(-1), ManagerId = 1, Manager = manager };

        _reviewRepoMock
            .Setup(r => r.GetPendingNearDeadlineAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new[] { MakeSession(report, WithinThreeDays) });

        var service = CreateService();

        // Act ────────────────────────────────────────────────────────────────
        await service.SendManagerSummariesAsync(_reviewRepoMock.Object);

        // Assert ─────────────────────────────────────────────────────────────
        _emailMock.Verify(
            e => e.SendEmailAsync("mike@example.com", "Manager Mike", It.IsAny<string>(), It.IsAny<string>()),
            Times.Once,
            "The manager must receive a summary when a team member has an overdue review.");
    }

    [Fact]
    public async Task SendManagerSummaries_DoesNotSendSummary_WhenRepositoryReturnsEmpty()
    {
        // Arrange ────────────────────────────────────────────────────────────
        _reviewRepoMock
            .Setup(r => r.GetPendingNearDeadlineAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(Enumerable.Empty<ReviewSession>());

        var service = CreateService();

        // Act ────────────────────────────────────────────────────────────────
        await service.SendManagerSummariesAsync(_reviewRepoMock.Object);

        // Assert ─────────────────────────────────────────────────────────────
        _emailMock.Verify(
            e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never,
            "No summary must be sent when the repository returns no overdue sessions.");
    }

    [Fact]
    public async Task SendManagerSummaries_DoesNotSendToManager_WhenEmployeeHasNoManager()
    {
        // Arrange ────────────────────────────────────────────────────────────
        // Top-level employee (no manager)
        var employee = new Employee { Id = 1, Name = "Solo Sue", Email = "sue@example.com", Position = "CTO", HireDate = FixedNow.UtcDateTime.AddYears(-5) };

        _reviewRepoMock
            .Setup(r => r.GetPendingNearDeadlineAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new[] { MakeSession(employee, WithinThreeDays) });

        var service = CreateService();

        // Act ────────────────────────────────────────────────────────────────
        await service.SendManagerSummariesAsync(_reviewRepoMock.Object);

        // Assert ─────────────────────────────────────────────────────────────
        _emailMock.Verify(
            e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never,
            "No email should be sent when the overdue employee has no manager.");
    }

    [Fact]
    public async Task SendManagerSummaries_SendsOneSummaryPerManager_ForMultipleReports()
    {
        // Arrange ────────────────────────────────────────────────────────────
        var manager = new Employee { Id = 1, Name = "Manager Leo", Email = "leo@example.com", Position = "Manager", HireDate = FixedNow.UtcDateTime.AddYears(-3) };
        var r1      = new Employee { Id = 2, Name = "Report One",  Email = "one@example.com",  Position = "Dev", HireDate = FixedNow.UtcDateTime.AddYears(-1), ManagerId = 1, Manager = manager };
        var r2      = new Employee { Id = 3, Name = "Report Two",  Email = "two@example.com",  Position = "Dev", HireDate = FixedNow.UtcDateTime.AddYears(-1), ManagerId = 1, Manager = manager };

        _reviewRepoMock
            .Setup(r => r.GetPendingNearDeadlineAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new[]
            {
                MakeSession(r1, WithinThreeDays),
                MakeSession(r2, WithinThreeDays)
            });

        var service = CreateService();

        // Act ────────────────────────────────────────────────────────────────
        await service.SendManagerSummariesAsync(_reviewRepoMock.Object);

        // Assert ─────────────────────────────────────────────────────────────
        _emailMock.Verify(
            e => e.SendEmailAsync("leo@example.com", "Manager Leo", It.IsAny<string>(), It.IsAny<string>()),
            Times.Once,
            "Each manager must receive exactly one summary regardless of how many reports are overdue.");
    }

    [Fact]
    public async Task SendManagerSummaries_PassesCorrectCutoffDate_ToRepository()
    {
        // Arrange ────────────────────────────────────────────────────────────
        // Fixed clock is 2026-03-15 → today.Date is 2026-03-15 → cutoff is 2026-03-18
        var expectedCutoff = new DateTime(2026, 3, 18, 0, 0, 0, DateTimeKind.Utc);

        _reviewRepoMock
            .Setup(r => r.GetPendingNearDeadlineAsync(expectedCutoff))
            .ReturnsAsync(Enumerable.Empty<ReviewSession>())
            .Verifiable("Repository must be queried with today + 3 days as the cutoff.");

        var service = CreateService();

        // Act ────────────────────────────────────────────────────────────────
        await service.SendManagerSummariesAsync(_reviewRepoMock.Object);

        // Assert ─────────────────────────────────────────────────────────────
        _reviewRepoMock.Verify();
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
    // RunScheduledChecksAsync — scope + repository resolution
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task RunScheduledChecks_CreatesNewScope_AndResolvesRepository()
    {
        // Arrange ────────────────────────────────────────────────────────────
        SetupScopeFactory();

        // Stub both repository calls to return empty so the service completes cleanly.
        _reviewRepoMock
            .Setup(r => r.GetPendingDueInRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(Enumerable.Empty<ReviewSession>());
        _reviewRepoMock
            .Setup(r => r.GetPendingNearDeadlineAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(Enumerable.Empty<ReviewSession>());

        var service = CreateService();

        // Act ────────────────────────────────────────────────────────────────
        await service.RunScheduledChecksAsync();

        // Assert ─────────────────────────────────────────────────────────────
        _scopeFactoryMock.Verify(f => f.CreateScope(), Times.Once,
            "A new DI scope must be created for each scheduled run.");
    }
}
