using System.Text.Json;
using System.Text.RegularExpressions;
using Tesseract;

namespace MGSPlus.Api.Services;

public record MedicationItem(string Name, string? Dosage, string? Frequency, string? Duration);

public class OcrService
{
    private readonly ILogger<OcrService> _logger;
    private readonly string _tessDataPath;

    public OcrService(ILogger<OcrService> logger, IConfiguration config)
    {
        _logger      = logger;
        _tessDataPath = config["Ocr:TessDataPath"]
            ?? Path.Combine(AppContext.BaseDirectory, "tessdata");
    }

    /// <summary>Extract text from an uploaded image using Tesseract OCR</summary>
    public string ExtractText(byte[] imageBytes)
    {
        if (!Directory.Exists(_tessDataPath))
        {
            _logger.LogWarning("tessdata directory not found at {Path} — OCR will return empty text", _tessDataPath);
            return string.Empty;
        }

        try
        {
            using var engine = new TesseractEngine(_tessDataPath, "vie+eng", EngineMode.Default);
            using var pix    = Pix.LoadFromMemory(imageBytes);
            using var page   = engine.Process(pix);
            return page.GetText();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OCR processing failed");
            throw;
        }
    }

    /// <summary>
    /// Parse raw OCR text to extract a medication list.
    /// Looks for lines matching common prescription patterns.
    /// </summary>
    public List<MedicationItem> ParseMedications(string rawText)
    {
        var results = new List<MedicationItem>();
        if (string.IsNullOrWhiteSpace(rawText)) return results;

        var lines = rawText.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Simple heuristic: lines that contain a capital word followed by numbers/dosage info
        var medPattern = new Regex(
            @"(?<name>[A-ZÀ-Ỵa-zà-ỵ][A-ZÀ-Ỵa-zà-ỵ\s\-]{2,40})\s+(?<dosage>\d+\s*mg|\d+\s*ml|\d+\s*mcg|\d+\s*g)?",
            RegexOptions.IgnoreCase);

        var dosePattern   = new Regex(@"\b(\d+\s*(?:mg|ml|mcg|g|viên|ống))\b", RegexOptions.IgnoreCase);
        var freqPattern   = new Regex(@"\b(\d+\s*lần\/ngày|\d+x\/day|sáng|trưa|tối|ngày \d+ lần)\b", RegexOptions.IgnoreCase);
        var durationPattern = new Regex(@"\b(\d+\s*(?:ngày|tuần|tháng|days|weeks|months))\b", RegexOptions.IgnoreCase);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.Length < 4) continue;

            // Skip lines that look like headers / totals
            if (Regex.IsMatch(trimmed, @"^(STT|Tên|SL|Đơn|Tổng|BS\.|Bác|Bệnh|Ngày|Họ)", RegexOptions.IgnoreCase))
                continue;

            var match = medPattern.Match(trimmed);
            if (!match.Success) continue;

            var name     = match.Groups["name"].Value.Trim();
            var dosage   = dosePattern.Match(trimmed).Value.Trim() is { Length: > 0 } d ? d : null;
            var freq     = freqPattern.Match(trimmed).Value.Trim()   is { Length: > 0 } f ? f : null;
            var duration = durationPattern.Match(trimmed).Value.Trim() is { Length: > 0 } du ? du : null;

            results.Add(new MedicationItem(name, dosage, freq, duration));
        }

        return results;
    }

    public static string SerializeMedications(IEnumerable<MedicationItem> items) =>
        JsonSerializer.Serialize(items);

    public static List<MedicationItem> DeserializeMedications(string? json)
    {
        if (string.IsNullOrEmpty(json)) return new();
        try { return JsonSerializer.Deserialize<List<MedicationItem>>(json) ?? new(); }
        catch { return new(); }
    }
}
