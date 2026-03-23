using PerformanceReviewApi.Models;

namespace PerformanceReviewApi.Repositories;

public interface IReviewSessionRepository
{
    Task<IEnumerable<ReviewSession>> GetAllAsync();
    Task<ReviewSession?> GetByIdAsync(int id);
    Task<IEnumerable<ReviewSession>> GetByEmployeeIdAsync(int employeeId);
    Task<ReviewSession> CreateAsync(ReviewSession session);

    /// <summary>Returns false when no session with the given id exists.</summary>
    Task<bool> UpdateAsync(ReviewSession session);

    /// <summary>Returns false when no session with the given id exists.</summary>
    Task<bool> DeleteAsync(int id);

    Task<bool> ExistsAsync(int id);

    /// <summary>
    /// Returns all Pending sessions whose Deadline falls within [from, to),
    /// with the Employee navigation property eagerly loaded.
    /// </summary>
    Task<IEnumerable<ReviewSession>> GetPendingDueInRangeAsync(DateTime from, DateTime to);

    /// <summary>
    /// Returns all Pending sessions whose Deadline (date part) is on or before
    /// <paramref name="deadlineCutoff"/>, with Employee and Employee.Manager loaded.
    /// </summary>
    Task<IEnumerable<ReviewSession>> GetPendingNearDeadlineAsync(DateTime deadlineCutoff);
}
