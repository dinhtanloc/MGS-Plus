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
public class BlogController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly JwtService _jwt;

    public BlogController(ApplicationDbContext db, JwtService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    /// <summary>Blog post list (public)</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetPosts(
        [FromQuery] int? categoryId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = _db.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Category)
            .Where(b => b.IsPublished);

        if (categoryId != null) query = query.Where(b => b.CategoryId == categoryId);
        if (!string.IsNullOrEmpty(search))
            query = query.Where(b => b.Title.Contains(search) || b.Summary!.Contains(search));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(b => b.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new BlogPostSummaryDto(
                b.Id, b.Title, b.Slug, b.Summary, b.ThumbnailUrl, b.Tags, b.ViewCount,
                b.Author.FirstName + " " + b.Author.LastName,
                b.Category == null ? null : b.Category.Name, b.PublishedAt))
            .ToListAsync();

        return Ok(new { total, page, pageSize, data = items });
    }

    /// <summary>Blog post detail by slug</summary>
    [HttpGet("{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var post = await _db.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Slug == slug && b.IsPublished);

        if (post == null) return NotFound();

        post.ViewCount++;
        await _db.SaveChangesAsync();

        return Ok(new BlogPostDto(
            post.Id, post.Title, post.Slug, post.Summary, post.Content,
            post.ThumbnailUrl, post.Tags, post.IsPublished, post.ViewCount,
            post.Author.FirstName + " " + post.Author.LastName,
            post.Category?.Name, post.PublishedAt, post.CreatedAt
        ));
    }

    /// <summary>Blog categories</summary>
    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _db.BlogCategories
            .Select(c => new { c.Id, c.Name, c.Slug, c.Description, PostCount = c.Posts.Count(p => p.IsPublished) })
            .ToListAsync();
        return Ok(categories);
    }

    /// <summary>Create a new blog post</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Doctor")]
    public async Task<IActionResult> CreatePost([FromBody] CreateBlogPostRequest req)
    {
        var userId = _jwt.GetUserIdFromToken(User)!.Value;
        var slug = GenerateSlug(req.Title);

        if (await _db.BlogPosts.AnyAsync(b => b.Slug == slug))
            slug = $"{slug}-{DateTime.UtcNow.Ticks}";

        var post = new Models.BlogPost
        {
            AuthorId = userId,
            CategoryId = req.CategoryId,
            Title = req.Title,
            Slug = slug,
            Content = req.Content,
            Summary = req.Summary,
            Tags = req.Tags,
            ThumbnailUrl = req.ThumbnailUrl,
            IsPublished = req.IsPublished,
            PublishedAt = req.IsPublished ? DateTime.UtcNow : null
        };

        _db.BlogPosts.Add(post);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetBySlug), new { slug = post.Slug }, new { post.Id, post.Slug });
    }

    /// <summary>Update a blog post</summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Doctor")]
    public async Task<IActionResult> UpdatePost(int id, [FromBody] UpdateBlogPostRequest req)
    {
        var userId = _jwt.GetUserIdFromToken(User);
        var post = await _db.BlogPosts.FindAsync(id);
        if (post == null) return NotFound();

        if (req.Title != null) post.Title = req.Title;
        if (req.Content != null) post.Content = req.Content;
        if (req.Summary != null) post.Summary = req.Summary;
        if (req.CategoryId != null) post.CategoryId = req.CategoryId;
        if (req.Tags != null) post.Tags = req.Tags;
        if (req.ThumbnailUrl != null) post.ThumbnailUrl = req.ThumbnailUrl;
        if (req.IsPublished != null)
        {
            post.IsPublished = req.IsPublished.Value;
            if (req.IsPublished.Value && post.PublishedAt == null)
                post.PublishedAt = DateTime.UtcNow;
        }
        post.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Delete a blog post</summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Doctor")]
    public async Task<IActionResult> DeletePost(int id)
    {
        var userId = _jwt.GetUserIdFromToken(User);
        var post   = await _db.BlogPosts.FindAsync(id);
        if (post == null) return NotFound();

        // Only the author or Admin can delete
        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin && post.AuthorId != userId)
            return Forbid();

        _db.BlogPosts.Remove(post);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static string GenerateSlug(string title)
    {
        var slug = title.ToLower()
            .Replace(" ", "-")
            .Replace("đ", "d").Replace("ă", "a").Replace("â", "a").Replace("ê", "e")
            .Replace("ô", "o").Replace("ơ", "o").Replace("ư", "u").Replace("ị", "i")
            .Replace("ọ", "o").Replace("ụ", "u").Replace("ẹ", "e").Replace("ạ", "a");
        return System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\-]", "");
    }
}
