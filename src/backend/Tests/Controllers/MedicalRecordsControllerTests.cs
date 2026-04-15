using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MGSPlus.Api.Controllers;
using MGSPlus.Api.Models;
using MGSPlus.Tests.Helpers;

namespace MGSPlus.Tests.Controllers;

/// <summary>
/// Unit tests for MedicalRecordsController — record retrieval and creation.
/// Uses EF Core InMemory.
/// </summary>
public class MedicalRecordsControllerTests
{
    // ── factory helpers ───────────────────────────────────────────────────────

    private static MedicalRecordsController BuildWithUser(
        int userId,
        string role,
        Api.Data.ApplicationDbContext db)
    {
        var jwt = DbHelper.CreateJwtService();
        var ctrl = new MedicalRecordsController(db, jwt);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
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

    // ── GetMyRecords ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMyRecords_EmptyDb_ReturnsZeroTotal()
    {
        var db = DbHelper.CreateInMemoryDb();
        var user = new User { Email = "p@e.com", FirstName = "P", LastName = "U", Role = "Patient", PasswordHash = "x" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var ctrl = BuildWithUser(user.Id, "Patient", db);
        var result = await ctrl.GetMyRecords() as OkObjectResult;
        Assert.NotNull(result);
        dynamic body = result!.Value!;
        Assert.Equal(0, (int)body.GetType().GetProperty("total")!.GetValue(body));
    }

    [Fact]
    public async Task GetMyRecords_OnlyReturnsOwnRecords()
    {
        var db = DbHelper.CreateInMemoryDb();
        var user1 = new User { Email = "p1@e.com", FirstName = "P1", LastName = "U", Role = "Patient", PasswordHash = "x" };
        var user2 = new User { Email = "p2@e.com", FirstName = "P2", LastName = "U", Role = "Patient", PasswordHash = "x" };
        db.Users.AddRange(user1, user2);
        await db.SaveChangesAsync();

        db.MedicalRecords.AddRange(
            new MedicalRecord { UserId = user1.Id, Diagnosis = "Flu", RecordDate = DateTime.UtcNow },
            new MedicalRecord { UserId = user2.Id, Diagnosis = "Cold", RecordDate = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var ctrl = BuildWithUser(user1.Id, "Patient", db);
        var result = await ctrl.GetMyRecords() as OkObjectResult;
        dynamic body = result!.Value!;
        Assert.Equal(1, (int)body.GetType().GetProperty("total")!.GetValue(body));
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_OwnRecord_Returns200()
    {
        var db = DbHelper.CreateInMemoryDb();
        var user = new User { Email = "u@e.com", FirstName = "U", LastName = "U", Role = "Patient", PasswordHash = "x" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var record = new MedicalRecord
        {
            UserId = user.Id,
            Diagnosis = "Headache",
            RecordDate = DateTime.UtcNow.Date
        };
        db.MedicalRecords.Add(record);
        await db.SaveChangesAsync();

        var ctrl = BuildWithUser(user.Id, "Patient", db);
        var result = await ctrl.GetById(record.Id) as OkObjectResult;
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetById_AnotherUsersRecord_ReturnsNotFound()
    {
        var db = DbHelper.CreateInMemoryDb();
        var owner = new User { Email = "owner@e.com", FirstName = "O", LastName = "U", Role = "Patient", PasswordHash = "x" };
        db.Users.Add(owner);
        await db.SaveChangesAsync();

        var record = new MedicalRecord { UserId = owner.Id, Diagnosis = "X", RecordDate = DateTime.UtcNow };
        db.MedicalRecords.Add(record);
        await db.SaveChangesAsync();

        var ctrl = BuildWithUser(9999, "Patient", db);
        var result = await ctrl.GetById(record.Id);
        Assert.IsType<NotFoundResult>(result);
    }

    // ── CreateRecord ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateRecord_AsDoctor_Returns201()
    {
        var db = DbHelper.CreateInMemoryDb();
        var patient = new User { Email = "pat@e.com", FirstName = "P", LastName = "U", Role = "Patient", PasswordHash = "x" };
        var docUser = new User { Email = "doc@e.com", FirstName = "D", LastName = "U", Role = "Doctor",  PasswordHash = "x" };
        db.Users.AddRange(patient, docUser);
        await db.SaveChangesAsync();

        var doctor = new Doctor { UserId = docUser.Id, Specialty = "Nội khoa", IsAvailable = true };
        db.Doctors.Add(doctor);
        await db.SaveChangesAsync();

        var ctrl = BuildWithUser(docUser.Id, "Doctor", db);
        var req = new CreateMedicalRecordRequest(
            patient.Id, null, "Diagnosis text", "Paracetamol", null, null, null, DateTime.UtcNow);

        var result = await ctrl.CreateRecord(req) as CreatedAtActionResult;
        Assert.NotNull(result);
        Assert.Equal(201, result!.StatusCode);
        Assert.Single(db.MedicalRecords);
    }

    [Fact]
    public async Task CreateRecord_DoctorIdLinkedFromUser()
    {
        var db = DbHelper.CreateInMemoryDb();
        var patient = new User { Email = "pp@e.com", FirstName = "P", LastName = "U", Role = "Patient", PasswordHash = "x" };
        var docUser = new User { Email = "dd@e.com", FirstName = "D", LastName = "U", Role = "Doctor",  PasswordHash = "x" };
        db.Users.AddRange(patient, docUser);
        await db.SaveChangesAsync();

        var doctor = new Doctor { UserId = docUser.Id, Specialty = "Da liễu", IsAvailable = true };
        db.Doctors.Add(doctor);
        await db.SaveChangesAsync();

        var ctrl = BuildWithUser(docUser.Id, "Doctor", db);
        await ctrl.CreateRecord(new CreateMedicalRecordRequest(
            patient.Id, null, "Acne", null, null, null, null, null));

        var record = db.MedicalRecords.First();
        Assert.Equal(doctor.Id, record.DoctorId);
    }
}
