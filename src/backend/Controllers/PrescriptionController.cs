using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MGSPlus.Api.Data;
using MGSPlus.Api.Models;
using MGSPlus.Api.Services;

namespace MGSPlus.Api.Controllers;

[ApiController]
[Route("api/prescriptions")]
[Authorize]
[Produces("application/json")]
public class PrescriptionController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly JwtService _jwt;
    private readonly OcrService _ocr;

    public PrescriptionController(ApplicationDbContext db, JwtService jwt, OcrService ocr)
    {
        _db  = db;
        _jwt = jwt;
        _ocr = ocr;
    }

    /// <summary>Upload a prescription image and run OCR</summary>
    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Vui lòng chọn file ảnh" });

        var allowed = new[] { "image/jpeg", "image/png", "image/webp", "image/bmp", "image/tiff" };
        if (!allowed.Contains(file.ContentType.ToLower()))
            return BadRequest(new { message = "Chỉ chấp nhận file ảnh (JPEG, PNG, WEBP, BMP, TIFF)" });

        var userId = _jwt.GetUserIdFromToken(User)!.Value;

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var bytes = ms.ToArray();

        var prescription = new Prescription
        {
            UserId           = userId,
            OriginalFileName = Path.GetFileName(file.FileName),
            Status           = "Pending",
            CreatedAt        = DateTime.UtcNow
        };

        _db.Prescriptions.Add(prescription);
        await _db.SaveChangesAsync();

        // Process OCR in background (non-blocking)
        _ = Task.Run(async () =>
        {
            using var scope = HttpContext.RequestServices.CreateScope();
            var db          = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var ocrSvc      = scope.ServiceProvider.GetRequiredService<OcrService>();

            var p = await db.Prescriptions.FindAsync(prescription.Id);
            if (p == null) return;

            try
            {
                var rawText   = ocrSvc.ExtractText(bytes);
                var meds      = ocrSvc.ParseMedications(rawText);

                p.RawOcrText      = rawText;
                p.MedicationsJson = OcrService.SerializeMedications(meds);
                p.Status          = "Processed";
            }
            catch (Exception ex)
            {
                p.Status       = "Failed";
                p.ErrorMessage = ex.Message[..Math.Min(ex.Message.Length, 500)];
            }

            await db.SaveChangesAsync();
        });

        return Ok(new
        {
            id       = prescription.Id,
            status   = "Pending",
            message  = "Đang xử lý OCR, vui lòng kiểm tra lại sau vài giây"
        });
    }

    /// <summary>Get OCR result for a prescription</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetResult(int id)
    {
        var userId = _jwt.GetUserIdFromToken(User)!.Value;
        var p = await _db.Prescriptions.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
        if (p == null) return NotFound();

        return Ok(new
        {
            id               = p.Id,
            originalFileName = p.OriginalFileName,
            status           = p.Status,
            rawOcrText       = p.RawOcrText,
            medications      = OcrService.DeserializeMedications(p.MedicationsJson),
            errorMessage     = p.ErrorMessage,
            createdAt        = p.CreatedAt
        });
    }

    /// <summary>List my prescription history</summary>
    [HttpGet]
    public async Task<IActionResult> GetMyPrescriptions([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userId = _jwt.GetUserIdFromToken(User)!.Value;

        var total = await _db.Prescriptions.CountAsync(p => p.UserId == userId);
        var raw = await _db.Prescriptions
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new { p.Id, p.OriginalFileName, p.Status, p.MedicationsJson, p.CreatedAt })
            .ToListAsync();

        var items = raw.Select(p => new
        {
            id               = p.Id,
            originalFileName = p.OriginalFileName,
            status           = p.Status,
            medicationCount  = OcrService.DeserializeMedications(p.MedicationsJson).Count,
            createdAt        = p.CreatedAt
        });

        return Ok(new { total, page, pageSize, data = items });
    }
}
