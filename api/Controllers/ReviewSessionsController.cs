using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerformanceReviewApi.Data;
using PerformanceReviewApi.Models;

namespace PerformanceReviewApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewSessionsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ReviewSessionsController(AppDbContext db) => _db = db;

    // GET /api/reviewsessions
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReviewSession>>> GetAll()
    {
        return await _db.ReviewSessions
            .Include(rs => rs.Employee)
            .ToListAsync();
    }

    // GET /api/reviewsessions/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReviewSession>> GetById(int id)
    {
        var session = await _db.ReviewSessions
            .Include(rs => rs.Employee)
            .FirstOrDefaultAsync(rs => rs.Id == id);

        return session is null ? NotFound() : Ok(session);
    }

    // GET /api/reviewsessions/employee/{employeeId}
    [HttpGet("employee/{employeeId:int}")]
    public async Task<ActionResult<IEnumerable<ReviewSession>>> GetByEmployee(int employeeId)
    {
        return await _db.ReviewSessions
            .Where(rs => rs.EmployeeId == employeeId)
            .Include(rs => rs.Employee)
            .ToListAsync();
    }

    // POST /api/reviewsessions
    [HttpPost]
    public async Task<ActionResult<ReviewSession>> Create(ReviewSession session)
    {
        _db.ReviewSessions.Add(session);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = session.Id }, session);
    }

    // PUT /api/reviewsessions/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ReviewSession session)
    {
        if (id != session.Id) return BadRequest();

        _db.Entry(session).State = EntityState.Modified;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _db.ReviewSessions.AnyAsync(rs => rs.Id == id)) return NotFound();
            throw;
        }

        return NoContent();
    }

    // DELETE /api/reviewsessions/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var session = await _db.ReviewSessions.FindAsync(id);
        if (session is null) return NotFound();

        _db.ReviewSessions.Remove(session);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
