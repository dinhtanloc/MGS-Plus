using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MGSPlus.Api.Controllers;
using MGSPlus.Api.DTOs;
using MGSPlus.Api.Models;
using MGSPlus.Tests.Helpers;

namespace MGSPlus.Tests.Controllers;

/// <summary>
/// Unit tests for AppointmentsController — appointment booking and management.
/// Uses EF Core InMemory.
/// </summary>
public class AppointmentsControllerTests
{
    // ── factory helpers ───────────────────────────────────────────────────────

    private static AppointmentsController BuildWithUser(int userId, out Api.Data.ApplicationDbContext db)
    {
        db = DbHelper.CreateInMemoryDb();
        var jwt = DbHelper.CreateJwtService();
        var ctrl = new AppointmentsController(db, jwt);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "Patient")
        };
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
            }
        };
        return ctrl;
    }

    private static AppointmentsController BuildAnonymous(out Api.Data.ApplicationDbContext db)
    {
        db = DbHelper.CreateInMemoryDb();
        var jwt = DbHelper.CreateJwtService();
        var ctrl = new AppointmentsController(db, jwt);
        ctrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return ctrl;
    }

    private static (User user, Doctor doctor) SeedDoctorAndUser(Api.Data.ApplicationDbContext db)
    {
        var user = new User { Email = "p@example.com", FirstName = "Patient", LastName = "U", Role = "Patient", PasswordHash = "x" };
        var docUser = new User { Email = "d@example.com", FirstName = "Doctor", LastName = "D", Role = "Doctor", PasswordHash = "x" };
        db.Users.AddRange(user, docUser);
        db.SaveChanges();

        var doctor = new Doctor { UserId = docUser.Id, Specialty = "Nội khoa", IsAvailable = true };
        db.Doctors.Add(doctor);
        db.SaveChanges();

        return (user, doctor);
    }

    // ── GetMyAppointments ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetMyAppointments_EmptyForUser_ReturnsZero()
    {
        var user = new User { Email = "u@e.com", FirstName = "A", LastName = "B", Role = "Patient", PasswordHash = "x" };
        var db = DbHelper.CreateInMemoryDb();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var jwt = DbHelper.CreateJwtService();
        var ctrl = new AppointmentsController(db, jwt);
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()) }, "Test"))
            }
        };

        var result = await ctrl.GetMyAppointments(null) as OkObjectResult;
        Assert.NotNull(result);
        dynamic body = result!.Value!;
        Assert.Equal(0, (int)body.GetType().GetProperty("total")!.GetValue(body));
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidRequest_Returns201WithQueueNumber()
    {
        var db = DbHelper.CreateInMemoryDb();
        var (user, doctor) = SeedDoctorAndUser(db);

        var jwt = DbHelper.CreateJwtService();
        var ctrl = new AppointmentsController(db, jwt);
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()) }, "Test"))
            }
        };

        var req = new CreateAppointmentRequest(DateTime.UtcNow.AddDays(1), doctor.Id, "Nội khoa", "Đau bụng");
        var result = await ctrl.Create(req) as CreatedAtActionResult;

        Assert.NotNull(result);
        Assert.Equal(201, result!.StatusCode);

        var dto = result.Value as AppointmentDto;
        Assert.NotNull(dto);
        Assert.Equal(1, dto!.QueueNumber);
        Assert.Equal("Pending", dto.Status);
    }

    [Fact]
    public async Task Create_TwoSameDay_SecondQueueNumberIsTwo()
    {
        var db = DbHelper.CreateInMemoryDb();
        var (user, doctor) = SeedDoctorAndUser(db);

        var jwt = DbHelper.CreateJwtService();
        var ctrl = new AppointmentsController(db, jwt);
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()) }, "Test"))
            }
        };

        var day = DateTime.UtcNow.AddDays(2).Date.AddHours(9);
        await ctrl.Create(new CreateAppointmentRequest(day, doctor.Id, null, null));
        var result = await ctrl.Create(new CreateAppointmentRequest(day.AddMinutes(30), doctor.Id, null, null)) as CreatedAtActionResult;
        var dto = result!.Value as AppointmentDto;
        Assert.Equal(2, dto!.QueueNumber);
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_OtherUsersAppointment_ReturnsNotFound()
    {
        var db = DbHelper.CreateInMemoryDb();
        var (_, doctor) = SeedDoctorAndUser(db);

        // Create appointment for user 1
        var user1 = db.Users.First(u => u.Role == "Patient");
        db.Appointments.Add(new Appointment { UserId = user1.Id, DoctorId = doctor.Id, ScheduledAt = DateTime.UtcNow.AddDays(1), Status = "Pending", QueueNumber = 1 });
        await db.SaveChangesAsync();

        var appt = db.Appointments.First();

        // Try to access as a different user (id = 9999)
        var jwt = DbHelper.CreateJwtService();
        var ctrl = new AppointmentsController(db, jwt);
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, "9999") }, "Test"))
            }
        };

        var result = await ctrl.GetById(appt.Id);
        Assert.IsType<NotFoundResult>(result);
    }

    // ── GetDoctors ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetDoctors_ReturnsAvailableOnly()
    {
        var db = DbHelper.CreateInMemoryDb();
        var u1 = new User { Email = "a@e.com", FirstName = "A", LastName = "B", Role = "Doctor", PasswordHash = "x" };
        var u2 = new User { Email = "b@e.com", FirstName = "C", LastName = "D", Role = "Doctor", PasswordHash = "x" };
        db.Users.AddRange(u1, u2);
        await db.SaveChangesAsync();
        db.Doctors.AddRange(
            new Doctor { UserId = u1.Id, Specialty = "Tim mạch", IsAvailable = true },
            new Doctor { UserId = u2.Id, Specialty = "Da liễu",  IsAvailable = false }
        );
        await db.SaveChangesAsync();

        var ctrl = BuildAnonymous(out var dbAnon);
        // Use db with seeded data
        var jwt = DbHelper.CreateJwtService();
        var ctrl2 = new AppointmentsController(db, jwt);
        ctrl2.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = await ctrl2.GetDoctors(null) as OkObjectResult;
        var list = result!.Value as System.Collections.IEnumerable;
        Assert.Single(list!.Cast<object>());
    }

    // ── Update (cancel) ────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_CancelPendingAppointment_Returns204()
    {
        var db = DbHelper.CreateInMemoryDb();
        var (user, doctor) = SeedDoctorAndUser(db);

        var appt = new Appointment { UserId = user.Id, DoctorId = doctor.Id, ScheduledAt = DateTime.UtcNow.AddDays(1), Status = "Pending", QueueNumber = 1 };
        db.Appointments.Add(appt);
        await db.SaveChangesAsync();

        var jwt = DbHelper.CreateJwtService();
        var ctrl = new AppointmentsController(db, jwt);
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()) }, "Test"))
            }
        };

        var result = await ctrl.Update(appt.Id, new UpdateAppointmentRequest(null, "Cancelled", null, null));
        Assert.IsType<NoContentResult>(result);

        var updated = await db.Appointments.FindAsync(appt.Id);
        Assert.Equal("Cancelled", updated!.Status);
    }
}
