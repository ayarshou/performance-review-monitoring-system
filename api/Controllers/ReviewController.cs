using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PerformanceReviewApi.DTOs;
using PerformanceReviewApi.Models;
using PerformanceReviewApi.Repositories;

namespace PerformanceReviewApi.Controllers;

/// <summary>
/// Handles authenticated review operations:
///   POST /api/reviews/{id}/submit  – submit (complete) a review session
///   GET  /api/reviews/team-status  – manager views their subordinates' review status
/// </summary>
[ApiController]
[Route("api/reviews")]
[Authorize]
public class ReviewController : ControllerBase
{
    private readonly IReviewSessionRepository _reviewRepo;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly IValidator<SubmitReviewRequest> _validator;

    public ReviewController(
        IReviewSessionRepository reviewRepo,
        IEmployeeRepository employeeRepo,
        IValidator<SubmitReviewRequest> validator)
    {
        _reviewRepo = reviewRepo;
        _employeeRepo = employeeRepo;
        _validator = validator;
    }

    /// <summary>
    /// Marks the specified review session as Completed and saves optional notes.
    /// Only authenticated users may call this endpoint.
    /// </summary>
    // POST /api/reviews/{id}/submit
    [HttpPost("{id:int}/submit")]
    public async Task<IActionResult> SubmitReview(int id, [FromBody] SubmitReviewRequest request)
    {
        var validation = await _validator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));

        var session = await _reviewRepo.GetByIdAsync(id);
        if (session is null) return NotFound(new { message = $"Review session {id} not found." });

        session.Status = ReviewStatus.Completed;
        session.Notes = request.Notes;
        await _reviewRepo.UpdateAsync(session);

        return NoContent();
    }

    /// <summary>
    /// Returns all direct subordinates of the authenticated manager,
    /// together with each subordinate's review session completion status.
    /// Requires the Manager role.
    /// </summary>
    // GET /api/reviews/team-status
    [HttpGet("team-status")]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<IEnumerable<SubordinateReviewStatusDto>>> GetTeamStatus()
    {
        var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;
        if (!int.TryParse(employeeIdClaim, out int managerId))
            return BadRequest(new { message = "Manager identity could not be resolved from token." });

        var subordinates = await _employeeRepo.GetSubordinatesWithReviewsAsync(managerId);

        var result = subordinates.Select(e => new SubordinateReviewStatusDto
        {
            EmployeeId = e.Id,
            EmployeeName = e.Name,
            EmployeeEmail = e.Email,
            Position = e.Position,
            ReviewSessions = e.ReviewSessions.Select(rs => new ReviewSessionSummary
            {
                ReviewSessionId = rs.Id,
                Status = rs.Status,
                ScheduledDate = rs.ScheduledDate,
                Deadline = rs.Deadline
            })
        });

        return Ok(result);
    }
}
