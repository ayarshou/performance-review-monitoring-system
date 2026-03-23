using PerformanceReviewApi.Repositories;

namespace PerformanceReviewApi.Services;

/// <summary>
/// Background service that runs daily to:
/// 1. Send email reminders to employees whose review is due this month and still Pending.
/// 2. Send a summary report to managers for team members who have not completed their
///    review within 3 days of the deadline.
/// </summary>
public class ReviewSchedulerService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEmailService _emailService;
    private readonly ILogger<ReviewSchedulerService> _logger;
    private readonly TimeProvider _timeProvider;

    public ReviewSchedulerService(
        IServiceScopeFactory scopeFactory,
        IEmailService emailService,
        ILogger<ReviewSchedulerService> logger,
        TimeProvider? timeProvider = null)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReviewSchedulerService starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunScheduledChecksAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error during scheduled review checks.");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }

        _logger.LogInformation("ReviewSchedulerService stopping.");
    }

    /// <summary>
    /// Core logic: creates a DI scope, resolves <see cref="IReviewSessionRepository"/>,
    /// and runs both notification passes.
    /// Exposed as public so unit tests can invoke it directly.
    /// </summary>
    public async Task RunScheduledChecksAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var reviewRepo = scope.ServiceProvider.GetRequiredService<IReviewSessionRepository>();

        await SendMonthlyReviewNotificationsAsync(reviewRepo);
        await SendManagerSummariesAsync(reviewRepo);
    }

    /// <summary>
    /// Finds employees with a Pending review session whose deadline falls in the
    /// current calendar month and sends each one an email reminder.
    /// </summary>
    public async Task SendMonthlyReviewNotificationsAsync(IReviewSessionRepository reviewRepo)
    {
        var today = _timeProvider.GetUtcNow().UtcDateTime;
        var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);

        var pendingSessions = await reviewRepo.GetPendingDueInRangeAsync(monthStart, monthEnd);

        _logger.LogInformation(
            "Found {Count} pending review(s) due this month.",
            pendingSessions.Count());

        foreach (var session in pendingSessions)
        {
            var employee = session.Employee;
            var subject = "Performance Review Reminder";
            var body =
                $"Hi {employee.Name},\n\n" +
                $"This is a reminder that your performance review is due by " +
                $"{session.Deadline:MMMM dd, yyyy}.\n\n" +
                "Please complete it at your earliest convenience.\n\n" +
                "Regards,\nPerformance Review System";

            await _emailService.SendEmailAsync(employee.Email, employee.Name, subject, body);
        }
    }

    /// <summary>
    /// Finds Pending review sessions whose deadline is within the next 3 days,
    /// groups them by manager, and sends each manager a summary email.
    /// </summary>
    public async Task SendManagerSummariesAsync(IReviewSessionRepository reviewRepo)
    {
        var today = _timeProvider.GetUtcNow().UtcDateTime.Date;
        var deadlineCutoff = today.AddDays(3);

        var overdueSessions = await reviewRepo.GetPendingNearDeadlineAsync(deadlineCutoff);

        var sessionList = overdueSessions.ToList();
        if (sessionList.Count == 0)
        {
            _logger.LogInformation("No overdue sessions found; skipping manager summaries.");
            return;
        }

        // Group by manager (sessions for employees without a manager are skipped)
        var byManager = sessionList
            .Where(rs => rs.Employee.Manager is not null)
            .GroupBy(rs => rs.Employee.Manager!);

        foreach (var group in byManager)
        {
            var manager = group.Key;
            var lines = group
                .Select(rs =>
                    $"  - {rs.Employee.Name} (deadline: {rs.Deadline:MMMM dd, yyyy})")
                .ToList();

            var subject = "Overdue Review Summary for Your Team";
            var body =
                $"Hi {manager.Name},\n\n" +
                "The following members of your team have not yet completed their " +
                "performance review and their deadline is within 3 days:\n\n" +
                string.Join("\n", lines) +
                "\n\nPlease follow up with them as soon as possible.\n\n" +
                "Regards,\nPerformance Review System";

            await _emailService.SendEmailAsync(manager.Email, manager.Name, subject, body);

            _logger.LogInformation(
                "Sent overdue summary to manager {ManagerName} for {Count} employee(s).",
                manager.Name,
                group.Count());
        }
    }
}
