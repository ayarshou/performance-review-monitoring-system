using PerformanceReviewApi.Models;

namespace PerformanceReviewApi.Repositories;

public interface IEmployeeRepository
{
    Task<IEnumerable<Employee>> GetAllAsync();
    Task<Employee?> GetByIdAsync(int id);
    Task<Employee> CreateAsync(Employee employee);

    /// <summary>Returns false when no employee with the given id exists.</summary>
    Task<bool> UpdateAsync(Employee employee);

    /// <summary>Returns false when no employee with the given id exists.</summary>
    Task<bool> DeleteAsync(int id);

    Task<bool> ExistsAsync(int id);

    /// <summary>
    /// Returns the direct subordinates of <paramref name="managerId"/>
    /// with their ReviewSessions eagerly loaded.
    /// </summary>
    Task<IEnumerable<Employee>> GetSubordinatesWithReviewsAsync(int managerId);
}
