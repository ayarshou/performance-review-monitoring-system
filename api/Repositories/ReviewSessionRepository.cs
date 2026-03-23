using Microsoft.EntityFrameworkCore;
using PerformanceReviewApi.Data;
using PerformanceReviewApi.Models;

namespace PerformanceReviewApi.Repositories;

public class ReviewSessionRepository : IReviewSessionRepository
{
    private readonly AppDbContext _db;

    public ReviewSessionRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<ReviewSession>> GetAllAsync() =>
        await _db.ReviewSessions
            .Include(rs => rs.Employee)
            .ToListAsync();

    public async Task<ReviewSession?> GetByIdAsync(int id) =>
        await _db.ReviewSessions
            .Include(rs => rs.Employee)
            .FirstOrDefaultAsync(rs => rs.Id == id);

    public async Task<IEnumerable<ReviewSession>> GetByEmployeeIdAsync(int employeeId) =>
        await _db.ReviewSessions
            .Where(rs => rs.EmployeeId == employeeId)
            .Include(rs => rs.Employee)
            .ToListAsync();

    public async Task<ReviewSession> CreateAsync(ReviewSession session)
    {
        _db.ReviewSessions.Add(session);
        await _db.SaveChangesAsync();
        return session;
    }

    public async Task<bool> UpdateAsync(ReviewSession session)
    {
        if (!await ExistsAsync(session.Id)) return false;
        _db.Entry(session).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var session = await _db.ReviewSessions.FindAsync(id);
        if (session is null) return false;
        _db.ReviewSessions.Remove(session);
        await _db.SaveChangesAsync();
        return true;
    }

    public Task<bool> ExistsAsync(int id) =>
        _db.ReviewSessions.AnyAsync(rs => rs.Id == id);

    public async Task<IEnumerable<ReviewSession>> GetPendingDueInRangeAsync(DateTime from, DateTime to) =>
        await _db.ReviewSessions
            .Include(rs => rs.Employee)
            .Where(rs =>
                rs.Status == ReviewStatus.Pending &&
                rs.Deadline >= from &&
                rs.Deadline < to)
            .ToListAsync();

    public async Task<IEnumerable<ReviewSession>> GetPendingNearDeadlineAsync(DateTime deadlineCutoff) =>
        await _db.ReviewSessions
            .Include(rs => rs.Employee)
                .ThenInclude(e => e.Manager)
            .Where(rs =>
                rs.Status == ReviewStatus.Pending &&
                rs.Deadline.Date <= deadlineCutoff)
            .ToListAsync();
}
