namespace PerformanceReviewApi.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// "Manager" or "Employee"
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Optional link to the Employee record for this user.
    /// Managers need this to look up their subordinates.
    /// </summary>
    public int? EmployeeId { get; set; }

    public Employee? Employee { get; set; }
}
