using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MGSPlus.Api.Data;
using MGSPlus.Api.Services;

namespace MGSPlus.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class MedicalRecordsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly JwtService _jwt;

    public MedicalRecordsController(ApplicationDbContext db, JwtService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    /// <summary>Get the current user's medical records</summary>
    [HttpGet]
    public async Task<IActionResult> GetMyRecords([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userId = _jwt.GetUserIdFromToken(User);
        var query = _db.MedicalRecords
            .Include(r => r.Doctor).ThenInclude(d => d!.User)
            .Where(r => r.UserId == userId);

        var total = await query.CountAsync();
        var records = await query
            .OrderByDescending(r => r.RecordDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new
            {
                r.Id, r.Diagnosis, r.Prescription, r.Notes, r.LabResults,
                r.AttachmentUrl, r.RecordDate,
                DoctorName = r.Doctor == null ? null : r.Doctor.User.FirstName + " " + r.Doctor.User.LastName,
                r.Doctor!.Specialty
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, data = records });
    }

    /// <summary>Medical record detail</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = _jwt.GetUserIdFromToken(User);
        var record = await _db.MedicalRecords
            .Include(r => r.Doctor).ThenInclude(d => d!.User)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

        if (record == null) return NotFound();

        return Ok(new
        {
            record.Id, record.Diagnosis, record.Prescription,
            record.Notes, record.LabResults, record.AttachmentUrl, record.RecordDate,
            Patient = record.User.FirstName + " " + record.User.LastName,
            DoctorName = record.Doctor == null ? null : record.Doctor.User.FirstName + " " + record.Doctor.User.LastName,
            record.Doctor?.Specialty
        });
    }

    /// <summary>Create a medical record (Doctor/Admin)</summary>
    [HttpPost]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> CreateRecord([FromBody] CreateMedicalRecordRequest req)
    {
        var doctorUserId = _jwt.GetUserIdFromToken(User)!.Value;
        var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.UserId == doctorUserId);

        var record = new Models.MedicalRecord
        {
            UserId = req.UserId,
            DoctorId = doctor?.Id,
            AppointmentId = req.AppointmentId,
            Diagnosis = req.Diagnosis,
            Prescription = req.Prescription,
            Notes = req.Notes,
            LabResults = req.LabResults,
            AttachmentUrl = req.AttachmentUrl,
            RecordDate = req.RecordDate ?? DateTime.UtcNow
        };

        _db.MedicalRecords.Add(record);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = record.Id }, new { record.Id });
    }
}

public record CreateMedicalRecordRequest(
    int UserId,
    int? AppointmentId,
    string? Diagnosis,
    string? Prescription,
    string? Notes,
    string? LabResults,
    string? AttachmentUrl,
    DateTime? RecordDate
);
