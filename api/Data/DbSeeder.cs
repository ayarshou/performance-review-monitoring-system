using PerformanceReviewApi.Helpers;
using PerformanceReviewApi.Models;

namespace PerformanceReviewApi.Data;

public static class DbSeeder
{
    public static void Seed(AppDbContext db)
    {
        // Only seed when the database is empty
        if (db.Employees.Any()) return;

        var today = DateTime.UtcNow.Date;

        var pw = PasswordHelper.Hash("Review2026!");

        // ── Managers ────────────────────────────────────────────────────────────
        var alice = new Employee { Name = "Alice Johnson",   Email = "alice.johnson@company.com",   Position = "Engineering Manager",  HireDate = today.AddYears(-6), Username = "alice", PasswordHash = pw };
        var bob   = new Employee { Name = "Bob Martinez",    Email = "bob.martinez@company.com",    Position = "Product Manager",       HireDate = today.AddYears(-5), Username = "bob",   PasswordHash = pw };
        var carol = new Employee { Name = "Carol Williams",  Email = "carol.williams@company.com",  Position = "HR Manager",            HireDate = today.AddYears(-7), Username = "carol", PasswordHash = pw };

        db.Employees.AddRange(alice, bob, carol);
        db.SaveChanges();

        // ── Employees reporting to Alice (Engineering) ──────────────────────────
        var eng1 = new Employee { Name = "David Lee",      Email = "david.lee@company.com",      Position = "Software Engineer",        HireDate = today.AddYears(-3),  ManagerId = alice.Id, Username = "david", PasswordHash = pw };
        var eng2 = new Employee { Name = "Eva Brown",      Email = "eva.brown@company.com",       Position = "Senior Software Engineer", HireDate = today.AddYears(-4),  ManagerId = alice.Id, Username = "eva",   PasswordHash = pw };
        var eng3 = new Employee { Name = "Frank Davis",    Email = "frank.davis@company.com",     Position = "Software Engineer",        HireDate = today.AddYears(-2),  ManagerId = alice.Id, Username = "frank", PasswordHash = pw };
        var eng4 = new Employee { Name = "Grace Kim",      Email = "grace.kim@company.com",       Position = "Junior Software Engineer", HireDate = today.AddYears(-1),  ManagerId = alice.Id, Username = "grace", PasswordHash = pw };

        // ── Employees reporting to Bob (Product) ────────────────────────────────
        var pm1  = new Employee { Name = "Henry Wilson",   Email = "henry.wilson@company.com",    Position = "Product Analyst",          HireDate = today.AddYears(-2),  ManagerId = bob.Id,   Username = "henry", PasswordHash = pw };
        var pm2  = new Employee { Name = "Isla Thompson",  Email = "isla.thompson@company.com",   Position = "UX Designer",              HireDate = today.AddYears(-3),  ManagerId = bob.Id,   Username = "isla",  PasswordHash = pw };
        var pm3  = new Employee { Name = "Jake Anderson",  Email = "jake.anderson@company.com",   Position = "Business Analyst",         HireDate = today.AddYears(-1),  ManagerId = bob.Id,   Username = "jake",  PasswordHash = pw };

        // ── Employees reporting to Carol (HR) ────────────────────────────────────
        var hr1  = new Employee { Name = "Karen White",    Email = "karen.white@company.com",     Position = "HR Specialist",            HireDate = today.AddYears(-2),  ManagerId = carol.Id, Username = "karen", PasswordHash = pw };
        var hr2  = new Employee { Name = "Liam Garcia",    Email = "liam.garcia@company.com",     Position = "Recruitment Coordinator",  HireDate = today.AddMonths(-8), ManagerId = carol.Id, Username = "liam",  PasswordHash = pw };
        var hr3  = new Employee { Name = "Mia Robinson",   Email = "mia.robinson@company.com",    Position = "HR Coordinator",           HireDate = today.AddMonths(-5), ManagerId = carol.Id, Username = "mia",   PasswordHash = pw };

        db.Employees.AddRange(eng1, eng2, eng3, eng4, pm1, pm2, pm3, hr1, hr2, hr3);
        db.SaveChanges();

        // ── Review sessions: all 10 employees have a Pending review ─────────────
        var employees = new[] { eng1, eng2, eng3, eng4, pm1, pm2, pm3, hr1, hr2, hr3 };

        foreach (var emp in employees)
        {
            db.ReviewSessions.Add(new ReviewSession
            {
                EmployeeId    = emp.Id,
                Status        = ReviewStatus.Pending,
                ScheduledDate = today.AddDays(7),
                Deadline      = today.AddDays(30),
            });
        }

        // ── A couple of completed reviews for context ────────────────────────────
        db.ReviewSessions.Add(new ReviewSession
        {
            EmployeeId    = eng2.Id,
            Status        = ReviewStatus.Completed,
            ScheduledDate = today.AddMonths(-6),
            Deadline      = today.AddMonths(-5),
        });
        db.ReviewSessions.Add(new ReviewSession
        {
            EmployeeId    = pm1.Id,
            Status        = ReviewStatus.Completed,
            ScheduledDate = today.AddMonths(-3),
            Deadline      = today.AddMonths(-2),
        });

        db.SaveChanges();
    }
}
