using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MGSPlus.Api.Data;
using MGSPlus.Api.DTOs;
using MGSPlus.Api.Services;

namespace MGSPlus.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class NewsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly JwtService _jwt;

    public NewsController(ApplicationDbContext db, JwtService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    /// <summary>News list (public)</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetNews(
        [FromQuery] int? categoryId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = _db.News
            .Include(n => n.Category)
            .Where(n => n.IsPublished);

        if (categoryId != null) query = query.Where(n => n.CategoryId == categoryId);
        if (!string.IsNullOrEmpty(search))
            query = query.Where(n => n.Title.Contains(search) || n.Summary!.Contains(search));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(n => n.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NewsSummaryDto(
                n.Id, n.Title, n.Summary, n.ImageUrl, n.Source,
                n.Category == null ? null : n.Category.Name, n.ViewCount, n.PublishedAt))
            .ToListAsync();

        return Ok(new { total, page, pageSize, data = items });
    }

    /// <summary>News item detail</summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var news = await _db.News
            .Include(n => n.Category)
            .FirstOrDefaultAsync(n => n.Id == id && n.IsPublished);

        if (news == null) return NotFound();

        news.ViewCount++;
        await _db.SaveChangesAsync();

        return Ok(new NewsDto(
            news.Id, news.Title, news.Summary, news.Content,
            news.ImageUrl, news.Source, news.Tags, news.ViewCount,
            news.Category?.Name, news.PublishedAt
        ));
    }

    /// <summary>News categories</summary>
    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategories()
    {
        var cats = await _db.NewsCategories
            .Select(c => new { c.Id, c.Name, c.Slug, Count = c.NewsItems.Count(n => n.IsPublished) })
            .ToListAsync();
        return Ok(cats);
    }

    /// <summary>Create a news item (Admin)</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateNews([FromBody] CreateNewsRequest req)
    {
        var news = new Models.News
        {
            Title = req.Title,
            Content = req.Content,
            Summary = req.Summary,
            CategoryId = req.CategoryId,
            Tags = req.Tags,
            ImageUrl = req.ImageUrl,
            Source = req.Source,
            SourceUrl = req.SourceUrl,
            IsPublished = true,
            PublishedAt = DateTime.UtcNow
        };

        _db.News.Add(news);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = news.Id }, new { news.Id });
    }

    /// <summary>Update a news item (Admin)</summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateNews(int id, [FromBody] UpdateNewsRequest req)
    {
        var news = await _db.News.FindAsync(id);
        if (news == null) return NotFound();

        if (req.Title      != null) news.Title      = req.Title;
        if (req.Content    != null) news.Content    = req.Content;
        if (req.Summary    != null) news.Summary    = req.Summary;
        if (req.ImageUrl   != null) news.ImageUrl   = req.ImageUrl;
        if (req.Tags       != null) news.Tags       = req.Tags;
        if (req.CategoryId != null) news.CategoryId = req.CategoryId;
        if (req.IsPublished.HasValue)
        {
            news.IsPublished = req.IsPublished.Value;
            if (req.IsPublished.Value && news.PublishedAt == null)
                news.PublishedAt = DateTime.UtcNow;
        }
        news.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Delete a news item (Admin)</summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteNews(int id)
    {
        var news = await _db.News.FindAsync(id);
        if (news == null) return NotFound();

        _db.News.Remove(news);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Featured news items (top by view count)</summary>
    [HttpGet("featured")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFeatured([FromQuery] int limit = 5)
    {
        var items = await _db.News
            .Include(n => n.Category)
            .Where(n => n.IsPublished)
            .OrderByDescending(n => n.ViewCount)
            .Take(limit)
            .Select(n => new NewsSummaryDto(
                n.Id, n.Title, n.Summary, n.ImageUrl, n.Source,
                n.Category == null ? null : n.Category.Name, n.ViewCount, n.PublishedAt))
            .ToListAsync();

        return Ok(items);
    }
}
