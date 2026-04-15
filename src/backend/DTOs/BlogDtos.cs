using System.ComponentModel.DataAnnotations;

namespace MGSPlus.Api.DTOs;

public record CreateBlogPostRequest(
    [Required, MaxLength(300)] string Title,
    [Required] string Content,
    string? Summary,
    int? CategoryId,
    string? Tags,
    string? ThumbnailUrl,
    bool IsPublished = false
);

public record UpdateBlogPostRequest(
    string? Title,
    string? Content,
    string? Summary,
    int? CategoryId,
    string? Tags,
    string? ThumbnailUrl,
    bool? IsPublished
);

public record BlogPostDto(
    int Id,
    string Title,
    string Slug,
    string? Summary,
    string Content,
    string? ThumbnailUrl,
    string? Tags,
    bool IsPublished,
    int ViewCount,
    string AuthorName,
    string? CategoryName,
    DateTime? PublishedAt,
    DateTime CreatedAt
);

public record BlogPostSummaryDto(
    int Id,
    string Title,
    string Slug,
    string? Summary,
    string? ThumbnailUrl,
    string? Tags,
    int ViewCount,
    string AuthorName,
    string? CategoryName,
    DateTime? PublishedAt
);

public record CreateNewsRequest(
    [Required, MaxLength(300)] string Title,
    [Required] string Content,
    string? Summary,
    int? CategoryId,
    string? Tags,
    string? ImageUrl,
    string? Source,
    string? SourceUrl
);

public record NewsDto(
    int Id,
    string Title,
    string? Summary,
    string Content,
    string? ImageUrl,
    string? Source,
    string? Tags,
    int ViewCount,
    string? CategoryName,
    DateTime? PublishedAt
);

public record UpdateNewsRequest(
    string? Title,
    string? Content,
    string? Summary,
    int? CategoryId,
    string? Tags,
    string? ImageUrl,
    bool? IsPublished
);

public record NewsSummaryDto(
    int Id,
    string Title,
    string? Summary,
    string? ImageUrl,
    string? Source,
    string? CategoryName,
    int ViewCount,
    DateTime? PublishedAt
);
