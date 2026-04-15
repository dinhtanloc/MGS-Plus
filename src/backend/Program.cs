using System.Text;
using System.Threading.RateLimiting;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MGSPlus.Api.Data;
using MGSPlus.Api.Middleware;
using MGSPlus.Api.Services;
using Prometheus;
using Serilog;
using Serilog.Events;

// Load root .env — traverse up from current directory until .env is found.
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

// ── Serilog bootstrap logger (before host build so startup errors are captured)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        "logs/mgsplus-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

// Replace default logging with Serilog (reads enrichers / sinks from above)
builder.Host.UseSerilog((ctx, services, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .ReadFrom.Services(services)
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        "logs/mgsplus-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}"));

// ── Configuration ────────────────────────────────────────────────────────────

// 1.1 — Fail-fast: JWT_SECRET must be set and at least 32 chars
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? builder.Configuration["Jwt:Secret"];

if (string.IsNullOrWhiteSpace(jwtSecret))
    throw new InvalidOperationException(
        "JWT_SECRET environment variable is required. Set it in your .env file (min 32 chars).");

if (jwtSecret.Length < 32)
    throw new InvalidOperationException(
        $"JWT_SECRET must be at least 32 characters (current: {jwtSecret.Length}).");

var jwtIssuer   = Environment.GetEnvironmentVariable("JWT_ISSUER")   ?? builder.Configuration["Jwt:Issuer"]   ?? "MGSPlus";
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")  ?? builder.Configuration["Jwt:Audience"] ?? "MGSPlusApp";

builder.Configuration["Jwt:Secret"]         = jwtSecret;
builder.Configuration["Jwt:Issuer"]         = jwtIssuer;
builder.Configuration["Jwt:Audience"]       = jwtAudience;
builder.Configuration["Jwt:ExpiresMinutes"] = Environment.GetEnvironmentVariable("JWT_EXPIRES_MINUTES") ?? "60";

// ── SQL Server ────────────────────────────────────────────────────────────────
var sqlHost = Environment.GetEnvironmentVariable("SQLSERVER_HOST") ?? "localhost";
var sqlPort = Environment.GetEnvironmentVariable("SQLSERVER_PORT") ?? "1433";
var sqlUser = Environment.GetEnvironmentVariable("SQL_ADMIN_USER") ?? "sa";
var sqlPass = Environment.GetEnvironmentVariable("SA_PASSWORD")
    ?? throw new InvalidOperationException(
        "SA_PASSWORD environment variable is required. Set it in your .env file.");
var sqlDb   = Environment.GetEnvironmentVariable("SQLSERVER_DB")   ?? "mgsplus_db";
var connStr = $"Server={sqlHost},{sqlPort};Database={sqlDb};User Id={sqlUser};Password={sqlPass};TrustServerCertificate=True;";

// SMTP configuration
builder.Configuration["Smtp:Host"] = Environment.GetEnvironmentVariable("SMTP_HOST") ?? "";
builder.Configuration["Smtp:Port"] = Environment.GetEnvironmentVariable("SMTP_PORT") ?? "587";
builder.Configuration["Smtp:User"] = Environment.GetEnvironmentVariable("SMTP_USER") ?? "";
builder.Configuration["Smtp:Pass"] = Environment.GetEnvironmentVariable("SMTP_PASS") ?? "";
builder.Configuration["Smtp:From"] = Environment.GetEnvironmentVariable("SMTP_FROM") ?? "";
builder.Configuration["App:BaseUrl"] = Environment.GetEnvironmentVariable("APP_BASE_URL") ?? "http://localhost:5000";

// Agent service
var agentApiKey = Environment.GetEnvironmentVariable("AGENT_API_KEY") ?? "";
builder.Configuration["AgentService:SupervisorUrl"] =
    $"http://localhost:{Environment.GetEnvironmentVariable("SUPERVISOR_PORT") ?? "8010"}";
builder.Configuration["AgentService:ApiKey"] = agentApiKey;

// ── Services ─────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseSqlServer(connStr));

builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<ChatbotService>();
builder.Services.AddScoped<EmailService>();

// 1.2 — ProblemDetails (RFC 7807 structured errors)
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// 1.3 — Rate Limiting (built-in .NET 7+, no extra NuGet needed)
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("authPolicy", opt =>
    {
        opt.Window            = TimeSpan.FromMinutes(1);
        opt.PermitLimit       = 10;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit        = 0;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtIssuer,
            ValidAudience            = jwtAudience,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
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
        Title       = "MGSPlus Medical API",
        Version     = "v1",
        Description = "MGSPlus Medical System API — health consultation, appointment booking, medical records, AI chatbot"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description  = "JWT Authorization header. Format: Bearer {token}",
        Name         = "Authorization",
        In           = ParameterLocation.Header,
        Type         = SecuritySchemeType.Http,
        Scheme       = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
});

// 1.4 — CORS: explicit methods (no AllowAnyMethod + AllowCredentials combination)
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowFrontend", p =>
    {
        var origins = new[]
        {
            "http://localhost:3000",
            "http://localhost:5173",
            "http://localhost:4173",
            Environment.GetEnvironmentVariable("FRONTEND_ORIGIN") ?? "http://localhost:3000"
        }.Distinct().ToArray();

        p.WithOrigins(origins)
         .AllowAnyHeader()
         .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
         .AllowCredentials();
    });
});

// ── Build & Configure ─────────────────────────────────────────────────────────
var app = builder.Build();

// Auto-migrate on startup (dev only)
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

// Serilog request logging (before exception handler so failed reqs are logged)
app.UseSerilogRequestLogging();

// 1.2 — Global exception handler (must be first middleware)
app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "MGSPlus API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowFrontend");

// 1.3 — Rate limiting middleware
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Prometheus metrics endpoint (scrape at /metrics)
app.UseHttpMetrics();
app.MapMetrics();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();
