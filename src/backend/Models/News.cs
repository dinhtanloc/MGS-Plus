using System.ComponentModel.DataAnnotations;

namespace MGSPlus.Api.Models;

public class NewsCategory
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(120)]
    public string Slug { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<News> NewsItems { get; set; } = new List<News>();
}

public class News
{
    public int Id { get; set; }
    public int? CategoryId { get; set; }

    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(600)]
    public string? Summary { get; set; }

    public string Content { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? ImageUrl { get; set; }

    [MaxLength(200)]
    public string? Source { get; set; }

    [MaxLength(512)]
    public string? SourceUrl { get; set; }

    [MaxLength(500)]
    public string? Tags { get; set; }

    public bool IsPublished { get; set; } = true;
    public int ViewCount { get; set; } = 0;

    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public NewsCategory? Category { get; set; }
}
