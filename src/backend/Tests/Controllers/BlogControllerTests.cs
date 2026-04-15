using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MGSPlus.Api.Controllers;
using MGSPlus.Api.DTOs;
using MGSPlus.Api.Models;
using MGSPlus.Tests.Helpers;

namespace MGSPlus.Tests.Controllers;

/// <summary>
/// Unit tests for BlogController.
/// Uses EF Core InMemory — no real SQL Server needed.
/// </summary>
public class BlogControllerTests
{
    // ── factories ─────────────────────────────────────────────────────────────

    private static BlogController BuildAnonymous(out Api.Data.ApplicationDbContext db)
    {
        db = DbHelper.CreateInMemoryDb();
        var jwt = DbHelper.CreateJwtService();
        var ctrl = new BlogController(db, jwt);
        ctrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return ctrl;
    }

    private static BlogController BuildWithUser(int userId, string role, out Api.Data.ApplicationDbContext db)
    {
        db = DbHelper.CreateInMemoryDb();
        var jwt = DbHelper.CreateJwtService();
        var ctrl = new BlogController(db, jwt);
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

    private static User SeedUser(Api.Data.ApplicationDbContext db)
    {
        var user = new User { Email = "author@example.com", FirstName = "Author", LastName = "U", Role = "Doctor", PasswordHash = "x" };
        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    // ── GetPosts ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPosts_EmptyDb_ReturnsEmptyList()
    {
        var ctrl = BuildAnonymous(out _);
        var result = await ctrl.GetPosts(null, null, 1, 10) as OkObjectResult;

        Assert.NotNull(result);
        dynamic body = result!.Value!;
        Assert.Equal(0, (int)body.GetType().GetProperty("total")!.GetValue(body));
    }

    [Fact]
    public async Task GetPosts_OnlyReturnsPublished()
    {
        var ctrl = BuildAnonymous(out var db);
        var user = SeedUser(db);

        db.BlogPosts.AddRange(
            new BlogPost { AuthorId = user.Id, Title = "Pub", Slug = "pub", Content = "c", IsPublished = true, PublishedAt = DateTime.UtcNow },
            new BlogPost { AuthorId = user.Id, Title = "Draft", Slug = "draft", Content = "c", IsPublished = false }
        );
        await db.SaveChangesAsync();

        var result = await ctrl.GetPosts(null, null) as OkObjectResult;
        dynamic body = result!.Value!;
        Assert.Equal(1, (int)body.GetType().GetProperty("total")!.GetValue(body));
    }

    // ── GetBySlug ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetBySlug_ExistingPublished_Returns200AndIncrementsViewCount()
    {
        var ctrl = BuildAnonymous(out var db);
        var user = SeedUser(db);

        db.BlogPosts.Add(new BlogPost
        {
            AuthorId = user.Id, Title = "Hello", Slug = "hello",
            Content = "c", IsPublished = true, PublishedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await ctrl.GetBySlug("hello") as OkObjectResult;
        Assert.NotNull(result);
        var post = db.BlogPosts.First(p => p.Slug == "hello");
        Assert.Equal(1, post.ViewCount);
    }

    [Fact]
    public async Task GetBySlug_NotFound_Returns404()
    {
        var ctrl = BuildAnonymous(out _);
        var result = await ctrl.GetBySlug("nope");
        Assert.IsType<NotFoundResult>(result);
    }

    // ── CreatePost ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreatePost_AsDoctor_Returns201()
    {
        var user = new User { Email = "doc@example.com", FirstName = "D", LastName = "U", Role = "Doctor", PasswordHash = "x" };
        var db = DbHelper.CreateInMemoryDb();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var ctrl = BuildWithUser(user.Id, "Doctor", out _);
        // replace db with the one that has the user
        var jwt = DbHelper.CreateJwtService();
        var ctrl2 = new BlogController(db, jwt);
        ctrl2.ControllerContext = ctrl.ControllerContext;

        var req = new CreateBlogPostRequest("Test Title", "Test content", null, null, null, false, null);
        var result = await ctrl2.CreatePost(req) as CreatedAtActionResult;

        Assert.NotNull(result);
        Assert.Equal(201, result!.StatusCode);
        Assert.Single(db.BlogPosts);
    }

    // ── DeletePost ────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeletePost_NonExistent_Returns404()
    {
        var ctrl = BuildWithUser(1, "Admin", out _);
        var result = await ctrl.DeletePost(999);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeletePost_AsAdmin_Returns204()
    {
        var user = new User { Email = "adm@example.com", FirstName = "A", LastName = "U", Role = "Admin", PasswordHash = "x" };
        var db = DbHelper.CreateInMemoryDb();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var post = new BlogPost { AuthorId = user.Id, Title = "T", Slug = "t", Content = "c", IsPublished = true };
        db.BlogPosts.Add(post);
        await db.SaveChangesAsync();

        var jwt = DbHelper.CreateJwtService();
        var ctrl = new BlogController(db, jwt);
        var claims = new[] {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role, "Admin")
        };
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
            }
        };

        var result = await ctrl.DeletePost(post.Id);
        Assert.IsType<NoContentResult>(result);
        Assert.Empty(db.BlogPosts);
    }
}
