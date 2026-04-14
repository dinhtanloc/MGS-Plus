using System.Text;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MGSPlus.Api.Data;
using MGSPlus.Api.Services;

// Load root .env — traverse up from current directory until .env is found.
// Works for: dotnet run (cwd = src/backend/), docker (cwd = /app), bin/Debug/
static string? FindRootEnv(string startDir)
{
    var dir = new DirectoryInfo(startDir);
    while (dir != null)
    {
        var candidate = Path.Combine(dir.FullName, ".env");
        if (File.Exists(candidate)) return candidate;
        dir = dir.Parent;
    }
    return null;
}
var envPath = FindRootEnv(Directory.GetCurrentDirectory());
if (envPath != null)
    Env.Load(envPath);

var builder = WebApplication.CreateBuilder(args);

// ── Configuration ────────────────────────────────────────────────────────────
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? builder.Configuration["Jwt:Secret"]
    ?? "default-dev-secret-change-in-production!!";
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? builder.Configuration["Jwt:Issuer"] ?? "MGSPlus";
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? builder.Configuration["Jwt:Audience"] ?? "MGSPlusApp";

// Override configuration with environment variables
builder.Configuration["Jwt:Secret"] = jwtSecret;
builder.Configuration["Jwt:Issuer"] = jwtIssuer;
builder.Configuration["Jwt:Audience"] = jwtAudience;
builder.Configuration["Jwt:ExpiresMinutes"] = Environment.GetEnvironmentVariable("JWT_EXPIRES_MINUTES") ?? "60";

// ── SQL Server connection string ──────────────────────────────────────────────
var sqlHost = Environment.GetEnvironmentVariable("SQLSERVER_HOST") ?? "localhost";
var sqlPort = Environment.GetEnvironmentVariable("SQLSERVER_PORT") ?? "1433";
var sqlUser = Environment.GetEnvironmentVariable("SQL_ADMIN_USER") ?? "sa";
var sqlPass = Environment.GetEnvironmentVariable("SA_PASSWORD") ?? "YourStrong!Passw0rd";
var sqlDb = Environment.GetEnvironmentVariable("SQLSERVER_DB") ?? "mgsplus_db";
var connStr = $"Server={sqlHost},{sqlPort};Database={sqlDb};User Id={sqlUser};Password={sqlPass};TrustServerCertificate=True;";

// Agent service
builder.Configuration["AgentService:SupervisorUrl"] =
    $"http://localhost:{Environment.GetEnvironmentVariable("SUPERVISOR_PORT") ?? "8010"}";

// ── Services ─────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseSqlServer(connStr));

builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<ChatbotService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ── Swagger ───────────────────────────────────────────────────────────────────
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MGSPlus Medical API",
        Version = "v1",
        Description = "API hệ thống y tế MGSPlus — tư vấn sức khỏe, đặt lịch khám, hồ sơ y tế, chatbot AI"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Format: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
});

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowFrontend", p =>
    {
        var origins = new[]
        {
            "http://localhost:3000",   // Vite dev server (default)
            "http://localhost:5173",   // Vite alt port
            "http://localhost:4173",   // Vite preview
            Environment.GetEnvironmentVariable("FRONTEND_ORIGIN") ?? "http://localhost:3000"
        }.Distinct().ToArray();

        p.WithOrigins(origins)
         .AllowAnyHeader()
         .AllowAnyMethod()
         .AllowCredentials();
    });
});

// ── Build & Configure ─────────────────────────────────────────────────────────
var app = builder.Build();

// Auto-migrate on startup (dev only) — non-fatal if DB is unavailable
if (app.Environment.IsDevelopment())
{
    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogWarning("DB migration skipped — database not reachable: {Message}", ex.Message);
    }
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "MGSPlus API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();
