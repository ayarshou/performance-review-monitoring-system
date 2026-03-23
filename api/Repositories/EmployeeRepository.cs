using Microsoft.EntityFrameworkCore;
using PerformanceReviewApi.Data;
using PerformanceReviewApi.Models;

namespace PerformanceReviewApi.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly AppDbContext _db;

    public EmployeeRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<Employee>> GetAllAsync() =>
        await _db.Employees
            .Include(e => e.Manager)
            .Include(e => e.Subordinates)
            .ToListAsync();

    public async Task<Employee?> GetByIdAsync(int id) =>
        await _db.Employees
            .Include(e => e.Manager)
            .Include(e => e.Subordinates)
            .Include(e => e.ReviewSessions)
            .FirstOrDefaultAsync(e => e.Id == id);

    public async Task<Employee> CreateAsync(Employee employee)
    {
        _db.Employees.Add(employee);
        await _db.SaveChangesAsync();
        return employee;
    }

    public async Task<bool> UpdateAsync(Employee employee)
    {
        if (!await ExistsAsync(employee.Id)) return false;
        _db.Entry(employee).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var employee = await _db.Employees.FindAsync(id);
        if (employee is null) return false;
        _db.Employees.Remove(employee);
        await _db.SaveChangesAsync();
        return true;
    }

    public Task<bool> ExistsAsync(int id) =>
        _db.Employees.AnyAsync(e => e.Id == id);
}
