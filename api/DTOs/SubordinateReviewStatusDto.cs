using PerformanceReviewApi.Models;

namespace PerformanceReviewApi.DTOs;

public class SubordinateReviewStatusDto
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeEmail { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public IEnumerable<ReviewSessionSummary> ReviewSessions { get; set; } = [];
}

public class ReviewSessionSummary
{
    public int ReviewSessionId { get; set; }
    public ReviewStatus Status { get; set; }
    public DateTime ScheduledDate { get; set; }
    public DateTime Deadline { get; set; }
}
