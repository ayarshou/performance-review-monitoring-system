using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerformanceReviewApi.Data;
using PerformanceReviewApi.Models;

namespace PerformanceReviewApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly AppDbContext _db;

    public EmployeesController(AppDbContext db) => _db = db;

    // GET /api/employees
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Employee>>> GetAll()
    {
        return await _db.Employees
            .Include(e => e.Manager)
            .Include(e => e.Subordinates)
            .ToListAsync();
    }

    // GET /api/employees/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Employee>> GetById(int id)
    {
        var employee = await _db.Employees
            .Include(e => e.Manager)
            .Include(e => e.Subordinates)
            .Include(e => e.ReviewSessions)
            .FirstOrDefaultAsync(e => e.Id == id);

        return employee is null ? NotFound() : Ok(employee);
    }

    // POST /api/employees
    [HttpPost]
    public async Task<ActionResult<Employee>> Create(Employee employee)
    {
        _db.Employees.Add(employee);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = employee.Id }, employee);
    }

    // PUT /api/employees/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Employee employee)
    {
        if (id != employee.Id) return BadRequest();

        _db.Entry(employee).State = EntityState.Modified;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _db.Employees.AnyAsync(e => e.Id == id)) return NotFound();
            throw;
        }

        return NoContent();
    }

    // DELETE /api/employees/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var employee = await _db.Employees.FindAsync(id);
        if (employee is null) return NotFound();

        _db.Employees.Remove(employee);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
