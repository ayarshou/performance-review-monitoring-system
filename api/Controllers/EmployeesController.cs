using Microsoft.AspNetCore.Mvc;
using PerformanceReviewApi.Models;
using PerformanceReviewApi.Repositories;

namespace PerformanceReviewApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeRepository _repo;

    public EmployeesController(IEmployeeRepository repo) => _repo = repo;

    // GET /api/employees
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Employee>>> GetAll() =>
        Ok(await _repo.GetAllAsync());

    // GET /api/employees/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Employee>> GetById(int id)
    {
        var employee = await _repo.GetByIdAsync(id);
        return employee is null ? NotFound() : Ok(employee);
    }

    // POST /api/employees
    [HttpPost]
    public async Task<ActionResult<Employee>> Create(Employee employee)
    {
        var created = await _repo.CreateAsync(employee);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // PUT /api/employees/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Employee employee)
    {
        if (id != employee.Id) return BadRequest();
        var updated = await _repo.UpdateAsync(employee);
        return updated ? NoContent() : NotFound();
    }

    // DELETE /api/employees/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _repo.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
