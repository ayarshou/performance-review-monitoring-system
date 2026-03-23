using Microsoft.AspNetCore.Mvc;
using PerformanceReviewApi.Models;
using PerformanceReviewApi.Repositories;

namespace PerformanceReviewApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewSessionsController : ControllerBase
{
    private readonly IReviewSessionRepository _repo;

    public ReviewSessionsController(IReviewSessionRepository repo) => _repo = repo;

    // GET /api/reviewsessions
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReviewSession>>> GetAll() =>
        Ok(await _repo.GetAllAsync());

    // GET /api/reviewsessions/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReviewSession>> GetById(int id)
    {
        var session = await _repo.GetByIdAsync(id);
        return session is null ? NotFound() : Ok(session);
    }

    // GET /api/reviewsessions/employee/{employeeId}
    [HttpGet("employee/{employeeId:int}")]
    public async Task<ActionResult<IEnumerable<ReviewSession>>> GetByEmployee(int employeeId) =>
        Ok(await _repo.GetByEmployeeIdAsync(employeeId));

    // POST /api/reviewsessions
    [HttpPost]
    public async Task<ActionResult<ReviewSession>> Create(ReviewSession session)
    {
        var created = await _repo.CreateAsync(session);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // PUT /api/reviewsessions/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ReviewSession session)
    {
        if (id != session.Id) return BadRequest();
        var updated = await _repo.UpdateAsync(session);
        return updated ? NoContent() : NotFound();
    }

    // DELETE /api/reviewsessions/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _repo.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
