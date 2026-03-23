using PerformanceReviewApi.Controllers;

namespace PerformanceReviewApi.Tests.Controllers;

public class ReviewSessionsControllerTests
{
    private static Employee MakeEmployee(string name = "Alice") => new()
    {
        Name = name, Email = $"{name.ToLower()}@co.com",
        Position = "Engineer", HireDate = DateTime.UtcNow.Date,
    };

    private static ReviewSession MakeSession(int employeeId, ReviewStatus status = ReviewStatus.Pending) => new()
    {
        EmployeeId    = employeeId,
        Status        = status,
        ScheduledDate = DateTime.UtcNow.AddDays(7),
        Deadline      = DateTime.UtcNow.AddDays(30),
    };

    [Fact]
    public async Task GetAll_ReturnsAllSessions()
    {
        using var db = TestDbContextFactory.Create();
        var emp = MakeEmployee();
        db.Employees.Add(emp);
        await db.SaveChangesAsync();
        db.ReviewSessions.AddRange(MakeSession(emp.Id), MakeSession(emp.Id, ReviewStatus.Completed));
        await db.SaveChangesAsync();

        var result = await new ReviewSessionsController(db).GetAll();

        Assert.Equal(2, result.Value!.Count());
    }

    [Fact]
    public async Task GetById_ExistingSession_ReturnsOk()
    {
        using var db = TestDbContextFactory.Create();
        var emp = MakeEmployee();
        db.Employees.Add(emp);
        await db.SaveChangesAsync();
        var session = MakeSession(emp.Id);
        db.ReviewSessions.Add(session);
        await db.SaveChangesAsync();

        var result = await new ReviewSessionsController(db).GetById(session.Id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(session.Id, Assert.IsType<ReviewSession>(ok.Value).Id);
    }

    [Fact]
    public async Task GetById_NonExistingSession_ReturnsNotFound()
    {
        using var db = TestDbContextFactory.Create();
        var result = await new ReviewSessionsController(db).GetById(999);
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetByEmployee_ReturnsOnlyThatEmployeesSessions()
    {
        using var db = TestDbContextFactory.Create();
        var emp1 = MakeEmployee("Alice");
        var emp2 = MakeEmployee("Bob");
        db.Employees.AddRange(emp1, emp2);
        await db.SaveChangesAsync();
        db.ReviewSessions.AddRange(MakeSession(emp1.Id), MakeSession(emp1.Id), MakeSession(emp2.Id));
        await db.SaveChangesAsync();

        var result = await new ReviewSessionsController(db).GetByEmployee(emp1.Id);

        Assert.Equal(2, result.Value!.Count());
        Assert.All(result.Value!, s => Assert.Equal(emp1.Id, s.EmployeeId));
    }

    [Fact]
    public async Task Create_ValidSession_ReturnsCreatedAndPersists()
    {
        using var db = TestDbContextFactory.Create();
        var emp = MakeEmployee();
        db.Employees.Add(emp);
        await db.SaveChangesAsync();

        var result = await new ReviewSessionsController(db).Create(MakeSession(emp.Id));

        Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(1, db.ReviewSessions.Count());
    }

    [Fact]
    public async Task Update_ExistingSession_ReturnsNoContentAndUpdatesStatus()
    {
        using var db = TestDbContextFactory.Create();
        var emp = MakeEmployee();
        db.Employees.Add(emp);
        await db.SaveChangesAsync();
        var session = MakeSession(emp.Id);
        db.ReviewSessions.Add(session);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        session.Status = ReviewStatus.Completed;
        var result = await new ReviewSessionsController(db).Update(session.Id, session);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(ReviewStatus.Completed, db.ReviewSessions.Find(session.Id)!.Status);
    }

    [Fact]
    public async Task Update_MismatchedId_ReturnsBadRequest()
    {
        using var db = TestDbContextFactory.Create();
        var emp = MakeEmployee();
        db.Employees.Add(emp);
        await db.SaveChangesAsync();
        var session = MakeSession(emp.Id);
        db.ReviewSessions.Add(session);
        await db.SaveChangesAsync();

        var result = await new ReviewSessionsController(db).Update(session.Id + 99, session);

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task Delete_ExistingSession_ReturnsNoContentAndRemoves()
    {
        using var db = TestDbContextFactory.Create();
        var emp = MakeEmployee();
        db.Employees.Add(emp);
        await db.SaveChangesAsync();
        var session = MakeSession(emp.Id);
        db.ReviewSessions.Add(session);
        await db.SaveChangesAsync();

        var result = await new ReviewSessionsController(db).Delete(session.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(0, db.ReviewSessions.Count());
    }

    [Fact]
    public async Task Delete_NonExistingSession_ReturnsNotFound()
    {
        using var db = TestDbContextFactory.Create();
        var result = await new ReviewSessionsController(db).Delete(999);
        Assert.IsType<NotFoundResult>(result);
    }
}
