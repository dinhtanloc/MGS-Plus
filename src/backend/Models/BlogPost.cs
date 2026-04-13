using System.ComponentModel.DataAnnotations;

namespace MGSPlus.Api.Models;

public class BlogCategory
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(120)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<BlogPost> Posts { get; set; } = new List<BlogPost>();
}

public class BlogPost
{
    public int Id { get; set; }
    public int AuthorId { get; set; }
    public int? CategoryId { get; set; }

    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(400)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(600)]
    public string? Summary { get; set; }

    public string Content { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? ThumbnailUrl { get; set; }

    [MaxLength(500)]
    public string? Tags { get; set; }

    public bool IsPublished { get; set; } = false;
    public int ViewCount { get; set; } = 0;

    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User Author { get; set; } = null!;
    public BlogCategory? Category { get; set; }
}
