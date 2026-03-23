namespace PerformanceReviewApi.Models;

public class ReviewSession
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public ReviewStatus Status { get; set; } = ReviewStatus.Pending;
    public DateTime ScheduledDate { get; set; }
    public DateTime Deadline { get; set; }

    // Navigation property
    public Employee Employee { get; set; } = null!;
}
