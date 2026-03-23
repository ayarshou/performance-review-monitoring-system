namespace PerformanceReviewApi.Models;

public class ReviewSession
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public ReviewStatus Status { get; set; } = ReviewStatus.Pending;
    public DateTime ScheduledDate { get; set; }
    public DateTime Deadline { get; set; }

    public string? Notes { get; set; }

    // Navigation property — nullable so model binding doesn't treat it as required
    public Employee? Employee { get; set; }
}
