using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MGSPlus.Api.Controllers;
using MGSPlus.Api.DTOs;
using MGSPlus.Api.Models;
using MGSPlus.Tests.Helpers;

namespace MGSPlus.Tests.Controllers;

/// <summary>
/// Unit tests for NewsController — CRUD operations using EF Core InMemory.
/// </summary>
public class NewsControllerTests
{
    private static NewsController BuildAdmin(out Api.Data.ApplicationDbContext db)
    {
        db = DbHelper.CreateInMemoryDb();
        var jwt = DbHelper.CreateJwtService();
        var ctrl = new NewsController(db, jwt);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "Admin")
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

    private static NewsController BuildAnonymous(out Api.Data.ApplicationDbContext db)
    {
        db = DbHelper.CreateInMemoryDb();
        var jwt = DbHelper.CreateJwtService();
        var ctrl = new NewsController(db, jwt);
        ctrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return ctrl;
    }

    // ── GetNews ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetNews_EmptyDb_ReturnsZeroTotal()
    {
        var ctrl = BuildAnonymous(out _);
        var result = await ctrl.GetNews(null, null) as OkObjectResult;
        Assert.NotNull(result);
        dynamic body = result!.Value!;
        Assert.Equal(0, (int)body.GetType().GetProperty("total")!.GetValue(body));
    }

    [Fact]
    public async Task GetNews_OnlyReturnsPublished()
    {
        var ctrl = BuildAnonymous(out var db);
        db.News.AddRange(
            new News { Title = "Pub", Content = "c", IsPublished = true,  PublishedAt = DateTime.UtcNow },
            new News { Title = "Hidden", Content = "c", IsPublished = false }
        );
        await db.SaveChangesAsync();

        var result = await ctrl.GetNews(null, null) as OkObjectResult;
        dynamic body = result!.Value!;
        Assert.Equal(1, (int)body.GetType().GetProperty("total")!.GetValue(body));
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingPublished_Returns200AndIncrementsViewCount()
    {
        var ctrl = BuildAnonymous(out var db);
        var news = new News { Title = "T", Content = "c", IsPublished = true, PublishedAt = DateTime.UtcNow };
        db.News.Add(news);
        await db.SaveChangesAsync();

        var result = await ctrl.GetById(news.Id) as OkObjectResult;
        Assert.NotNull(result);
        var updated = await db.News.FindAsync(news.Id);
        Assert.Equal(1, updated!.ViewCount);
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        var ctrl = BuildAnonymous(out _);
        var result = await ctrl.GetById(999);
        Assert.IsType<NotFoundResult>(result);
    }

    // ── CreateNews ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateNews_AsAdmin_Returns201AndPersists()
    {
        var ctrl = BuildAdmin(out var db);
        var req = new CreateNewsRequest("Breaking News", "Full content", "Summary", null, null, null, null, null);

        var result = await ctrl.CreateNews(req) as CreatedAtActionResult;
        Assert.NotNull(result);
        Assert.Equal(201, result!.StatusCode);
        Assert.Single(db.News);
    }

    // ── UpdateNews ────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateNews_ExistingItem_Returns204()
    {
        var ctrl = BuildAdmin(out var db);
        var news = new News { Title = "Old", Content = "c", IsPublished = true, PublishedAt = DateTime.UtcNow };
        db.News.Add(news);
        await db.SaveChangesAsync();

        var result = await ctrl.UpdateNews(news.Id, new UpdateNewsRequest("New Title", null, null, null, null, null, null));
        Assert.IsType<NoContentResult>(result);

        var updated = await db.News.FindAsync(news.Id);
        Assert.Equal("New Title", updated!.Title);
    }

    [Fact]
    public async Task UpdateNews_NonExistent_Returns404()
    {
        var ctrl = BuildAdmin(out _);
        var result = await ctrl.UpdateNews(999, new UpdateNewsRequest(null, null, null, null, null, null, null));
        Assert.IsType<NotFoundResult>(result);
    }

    // ── DeleteNews ────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteNews_ExistingItem_Returns204AndRemoves()
    {
        var ctrl = BuildAdmin(out var db);
        var news = new News { Title = "T", Content = "c", IsPublished = true, PublishedAt = DateTime.UtcNow };
        db.News.Add(news);
        await db.SaveChangesAsync();

        var result = await ctrl.DeleteNews(news.Id);
        Assert.IsType<NoContentResult>(result);
        Assert.Empty(db.News);
    }

    [Fact]
    public async Task DeleteNews_NonExistent_Returns404()
    {
        var ctrl = BuildAdmin(out _);
        var result = await ctrl.DeleteNews(999);
        Assert.IsType<NotFoundResult>(result);
    }
}
