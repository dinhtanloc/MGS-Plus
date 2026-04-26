using System.Text;
using System.Threading.RateLimiting;
using DotNetEnv;
using MGSPlus.Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MGSPlus.Api.Data;
using MGSPlus.Api.Hubs;
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
// Non-secret defaults come from configs/*.yml; secrets from .env only.
// Priority: env var > YAML > hardcoded fallback.

// 1.1 — Fail-fast: JWT_SECRET must be set and at least 32 chars (secret → .env only)
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? builder.Configuration["Jwt:Secret"];

if (string.IsNullOrWhiteSpace(jwtSecret))
    throw new InvalidOperationException(
        "JWT_SECRET environment variable is required. Set it in your .env file (min 32 chars).");

if (jwtSecret.Length < 32)
    throw new InvalidOperationException(
        $"JWT_SECRET must be at least 32 characters (current: {jwtSecret.Length}).");

// JWT non-secret defaults → backend-config.yml
var jwtIssuer   = YamlConfig.Get("JWT_ISSUER",          "backend-config.yml", "jwt.issuer",          "MGSPlus");
var jwtAudience = YamlConfig.Get("JWT_AUDIENCE",         "backend-config.yml", "jwt.audience",        "MGSPlusApp");
var jwtExpires  = YamlConfig.Get("JWT_EXPIRES_MINUTES",  "backend-config.yml", "jwt.expires_minutes", "60");

builder.Configuration["Jwt:Secret"]         = jwtSecret;
builder.Configuration["Jwt:Issuer"]         = jwtIssuer;
builder.Configuration["Jwt:Audience"]       = jwtAudience;
builder.Configuration["Jwt:ExpiresMinutes"] = jwtExpires;

// ── SQL Server ────────────────────────────────────────────────────────────────
// Host/port/db → infra-config.yml; password → .env (secret)
var sqlHost = YamlConfig.Get("SQLSERVER_HOST", "infra-config.yml", "databases.sqlserver.host", "localhost");
var sqlPort = YamlConfig.Get("SQLSERVER_PORT", "infra-config.yml", "databases.sqlserver.port", "1433");
var sqlUser = YamlConfig.Get("SQL_ADMIN_USER", "infra-config.yml", "databases.sqlserver.user", "sa");
var sqlDb   = YamlConfig.Get("SQLSERVER_DB",   "infra-config.yml", "databases.sqlserver.name", "mgsplus_db");
var sqlPass = Environment.GetEnvironmentVariable("SA_PASSWORD")
    ?? throw new InvalidOperationException(
        "SA_PASSWORD environment variable is required. Set it in your .env file.");
var connStr = $"Server={sqlHost},{sqlPort};Database={sqlDb};User Id={sqlUser};Password={sqlPass};TrustServerCertificate=True;";

// ── App URLs ──────────────────────────────────────────────────────────────────
var backendPort  = YamlConfig.Get("BACKEND_PORT",  "backend-config.yml",  "service.port",  "5001");
var frontendPort = YamlConfig.Get("FRONTEND_PORT", "frontend-config.yml", "service.port",  "3000");
builder.Configuration["App:BaseUrl"]     = Environment.GetEnvironmentVariable("APP_BASE_URL")
    ?? $"http://localhost:{backendPort}";
builder.Configuration["App:FrontendUrl"] = Environment.GetEnvironmentVariable("APP_FRONTEND_URL")
    ?? $"http://localhost:{frontendPort}";

// ── SMTP (secrets → .env) ─────────────────────────────────────────────────────
builder.Configuration["Smtp:Host"] = Environment.GetEnvironmentVariable("SMTP_HOST") ?? "";
builder.Configuration["Smtp:Port"] = Environment.GetEnvironmentVariable("SMTP_PORT") ?? "587";
builder.Configuration["Smtp:User"] = Environment.GetEnvironmentVariable("SMTP_USER") ?? "";
builder.Configuration["Smtp:Pass"] = Environment.GetEnvironmentVariable("SMTP_PASS") ?? "";
builder.Configuration["Smtp:From"] = Environment.GetEnvironmentVariable("SMTP_FROM") ?? "";

// ── Agent service → agents-config.yml ────────────────────────────────────────
var supervisorPort = YamlConfig.Get("SUPERVISOR_PORT", "agents-config.yml", "services.supervisor.port", "8010");
var agentApiKey    = Environment.GetEnvironmentVariable("AGENT_API_KEY") ?? "";
builder.Configuration["AgentService:SupervisorUrl"] = $"http://localhost:{supervisorPort}";
builder.Configuration["AgentService:ApiKey"]        = agentApiKey;

// ── Services ─────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseSqlServer(connStr));

builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<ChatbotService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<OcrService>();
builder.Services.AddSignalR();

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
        // Allow SignalR to receive JWT from query string (WebSocket doesn't support headers)
        opt.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) &&
                    ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    ctx.Token = token;
                return Task.CompletedTask;
            }
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
        // CORS origins → backend-config.yml; env var adds an extra origin if needed
        var yamlOrigins = YamlConfig.GetList("backend-config.yml", "cors.allowed_origins");
        var extraOrigin = Environment.GetEnvironmentVariable("FRONTEND_ORIGIN");
        var origins = yamlOrigins
            .Concat(extraOrigin is not null ? [extraOrigin] : [])
            .Distinct()
            .ToArray();

        if (origins.Length == 0)
            origins = ["http://localhost:3000"];

        p.WithOrigins(origins)
         .AllowAnyHeader()
         .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
         .AllowCredentials();
    });
});

// ── Build & Configure ─────────────────────────────────────────────────────────
var app = builder.Build();

// Auto-migrate + seed admin on startup
try
{
    using var scope = app.Services.CreateScope();
    var db     = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    db.Database.Migrate();

    // Create admin from env vars if no admin exists yet
    var adminEmail    = Environment.GetEnvironmentVariable("ADMIN_EMAIL")    ?? "admin@mgsplus.vn";
    var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "";

    if (!string.IsNullOrWhiteSpace(adminPassword) && !db.Users.Any(u => u.Role == "Admin"))
    {
        db.Users.Add(new MGSPlus.Api.Models.User
        {
            Email           = adminEmail,
            PasswordHash    = BCrypt.Net.BCrypt.HashPassword(adminPassword),
            FirstName       = "System",
            LastName        = "Admin",
            Role            = "Admin",
            IsActive        = true,
            IsEmailVerified = true,
            CreatedAt       = DateTime.UtcNow,
            UpdatedAt       = DateTime.UtcNow
        });
        db.SaveChanges();
        logger.LogInformation("Admin account created: {Email}", adminEmail);
    }
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogWarning("DB startup skipped — database not reachable: {Message}", ex.Message);
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
app.MapHub<DirectChatHub>("/hubs/direct-chat");

// Prometheus metrics endpoint (scrape at /metrics)
app.UseHttpMetrics();
app.MapMetrics();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();
