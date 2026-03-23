using PerformanceReviewApi.Controllers;

namespace PerformanceReviewApi.Tests.Controllers;

public class EmployeesControllerTests
{
    private static Employee MakeEmployee(string name = "Alice", int? managerId = null) => new()
    {
        Name = name, Email = $"{name.ToLower()}@company.com",
        Position = "Engineer", HireDate = DateTime.UtcNow.Date, ManagerId = managerId,
    };

    [Fact]
    public async Task GetAll_EmptyDatabase_ReturnsEmptyList()
    {
        using var db = TestDbContextFactory.Create();
        var result = await new EmployeesController(db).GetAll();
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task GetAll_WithEmployees_ReturnsAllEmployees()
    {
        using var db = TestDbContextFactory.Create();
        db.Employees.AddRange(MakeEmployee("Alice"), MakeEmployee("Bob"));
        await db.SaveChangesAsync();

        var result = await new EmployeesController(db).GetAll();

        Assert.Equal(2, result.Value!.Count());
    }

    [Fact]
    public async Task GetById_ExistingId_ReturnsEmployee()
    {
        using var db = TestDbContextFactory.Create();
        var emp = MakeEmployee("Grace");
        db.Employees.Add(emp);
        await db.SaveChangesAsync();

        var result = await new EmployeesController(db).GetById(emp.Id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(emp.Id, Assert.IsType<Employee>(ok.Value).Id);
    }

    [Fact]
    public async Task GetById_NonExistingId_ReturnsNotFound()
    {
        using var db = TestDbContextFactory.Create();
        var result = await new EmployeesController(db).GetById(999);
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_ValidEmployee_ReturnsCreatedAndPersists()
    {
        using var db = TestDbContextFactory.Create();
        var result = await new EmployeesController(db).Create(MakeEmployee());

        Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(1, db.Employees.Count());
    }

    [Fact]
    public async Task Update_ExistingEmployee_ReturnsNoContentAndUpdatesName()
    {
        using var db = TestDbContextFactory.Create();
        var emp = MakeEmployee("Alice");
        db.Employees.Add(emp);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        emp.Name = "Alice Updated";
        var result = await new EmployeesController(db).Update(emp.Id, emp);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal("Alice Updated", db.Employees.Find(emp.Id)!.Name);
    }

    [Fact]
    public async Task Update_MismatchedId_ReturnsBadRequest()
    {
        using var db = TestDbContextFactory.Create();
        var emp = MakeEmployee();
        db.Employees.Add(emp);
        await db.SaveChangesAsync();

        var result = await new EmployeesController(db).Update(emp.Id + 99, emp);

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task Delete_ExistingEmployee_ReturnsNoContentAndRemoves()
    {
        using var db = TestDbContextFactory.Create();
        var emp = MakeEmployee();
        db.Employees.Add(emp);
        await db.SaveChangesAsync();

        var result = await new EmployeesController(db).Delete(emp.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(0, db.Employees.Count());
    }

    [Fact]
    public async Task Delete_NonExistingEmployee_ReturnsNotFound()
    {
        using var db = TestDbContextFactory.Create();
        var result = await new EmployeesController(db).Delete(999);
        Assert.IsType<NotFoundResult>(result);
    }
}
