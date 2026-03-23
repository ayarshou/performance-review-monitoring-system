namespace PerformanceReviewApi.Models;

public class Employee
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public DateTime HireDate { get; set; }

    /// <summary>
    /// Foreign key to the Manager (also an Employee). Null for top-level employees.
    /// </summary>
    public int? ManagerId { get; set; }

    // Self-referencing navigation properties
    public Employee? Manager { get; set; }
    public ICollection<Employee> Subordinates { get; set; } = new List<Employee>();

    // One-to-many: an employee can have many review sessions
    public ICollection<ReviewSession> ReviewSessions { get; set; } = new List<ReviewSession>();
}
