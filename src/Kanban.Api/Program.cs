using System.Data;
using System.Diagnostics;
using System.Reflection;
using Asp.Versioning;
using DbUp;
using DbUp.Sqlite;
using Kanban.Api.ErrorHandling;
using Kanban.Api.Health;
using Kanban.Api.Options;
using Kanban.DataAccess;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using NetEscapades.AspNetCore.SecurityHeaders;
using Scalar.AspNetCore;
using CorsOptions = Kanban.Api.Options.CorsOptions;

var builder = WebApplication.CreateBuilder(args);

// ── Options — fail at boot if configuration is missing ────────────────────────

builder.Services.AddOptions<GoogleAuthOptions>()
    .Bind(builder.Configuration.GetSection(GoogleAuthOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<SeedOptions>()
    .Bind(builder.Configuration.GetSection(SeedOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<CorsOptions>()
    .Bind(builder.Configuration.GetSection(CorsOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<ConnectionStringOptions>()
    .Bind(builder.Configuration.GetSection(ConnectionStringOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// ── Database ──────────────────────────────────────────────────────────────────

DapperConfiguration.RegisterTypeHandlers();

var connectionString = builder.Configuration.GetConnectionString("Kanban")
    ?? throw new InvalidOperationException("ConnectionStrings:Kanban is required.");

builder.Services.AddScoped<IDbConnection>(_ =>
{
    var conn = new SqliteConnection(connectionString);
    conn.Open();
    return conn;
});

// ── Exception Handling ────────────────────────────────────────────────────────

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddExceptionHandler<InfrastructureExceptionHandler>();
builder.Services.AddExceptionHandler<FallbackExceptionHandler>();

// ── CORS ──────────────────────────────────────────────────────────────────────

builder.Services.AddCors(options =>
    options.AddPolicy("KanbanWebApp", policy =>
        policy
            .WithOrigins(
                builder.Configuration
                    .GetSection("Cors:AllowedOrigins")
                    .Get<string[]>() ?? [])
            .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS")
            .AllowAnyHeader()
            .WithExposedHeaders("X-Correlation-Id", "api-supported-versions")));

// ── API Versioning ────────────────────────────────────────────────────────────

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

// ── OpenAPI — development only ────────────────────────────────────────────────

if (builder.Environment.IsDevelopment())
    builder.Services.AddOpenApi();

// ── Health Checks ─────────────────────────────────────────────────────────────

builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["live"])
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

// ── Rate Limiting ─────────────────────────────────────────────────────────────

builder.Services.AddRateLimiter(opts =>
{
    opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opts.OnRejected = async (ctx, _) =>
    {
        ctx.HttpContext.Response.Headers.RetryAfter = "60";
        await Results.Problem(
                title: "Too many requests",
                statusCode: StatusCodes.Status429TooManyRequests,
                detail: "Please retry after the Retry-After interval.")
            .ExecuteAsync(ctx.HttpContext);
    };

    opts.AddFixedWindowLimiter("anonymous", o =>
    {
        o.PermitLimit = 10;
        o.Window = TimeSpan.FromMinutes(1);
    });

    opts.AddSlidingWindowLimiter("authenticated", o =>
    {
        o.PermitLimit = 100;
        o.Window = TimeSpan.FromMinutes(1);
        o.SegmentsPerWindow = 6;
    });

    opts.AddSlidingWindowLimiter("mutating", o =>
    {
        o.PermitLimit = 30;
        o.Window = TimeSpan.FromMinutes(1);
        o.SegmentsPerWindow = 6;
    });
});

// ── Build ─────────────────────────────────────────────────────────────────────

var app = builder.Build();

// ── Migrations ────────────────────────────────────────────────────────────────

var seedOptions = app.Services.GetRequiredService<IOptions<SeedOptions>>().Value;
var migrationsAssembly = AppDomain.CurrentDomain.GetAssemblies()
    .FirstOrDefault(a => a.GetName().Name == "Kanban.Data")
    ?? Assembly.Load("Kanban.Data");

var upgradeResult = DeployChanges.To
    .SqliteDatabase(connectionString)
    .WithScriptsEmbeddedInAssembly(
        migrationsAssembly,
        name => name.Contains("sqlite", StringComparison.OrdinalIgnoreCase))
    .WithVariables(new Dictionary<string, string>
    {
        ["AdminEmail"] = seedOptions.AdminEmail,
        ["AdminUserId"] = Guid.NewGuid().ToString("D"),
        ["SeedTimestamp"] = DateTimeOffset.UtcNow.ToString("o"),
    })
    .LogToConsole()
    .Build()
    .PerformUpgrade();

if (!upgradeResult.Successful)
    throw new InvalidOperationException($"Database migration failed: {upgradeResult.Error?.Message}");

// ── Middleware pipeline ───────────────────────────────────────────────────────
// Order matters — see constitution Security Headers and Containerization sections.

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.Use(async (ctx, next) =>
{
    var correlationId = ctx.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                        ?? Activity.Current?.Id
                        ?? Guid.NewGuid().ToString("N");
    Activity.Current?.SetTag("correlation.id", correlationId);
    ctx.Response.Headers["X-Correlation-Id"] = correlationId;
    await next();
});

app.UseSecurityHeaders(policies =>
    policies
        .AddDefaultSecurityHeaders()
        .AddStrictTransportSecurityMaxAgeIncludeSubDomains(maxAgeInSeconds: 60 * 60 * 24 * 365)
        .AddContentSecurityPolicy(csp =>
        {
            csp.AddDefaultSrc().Self();
            csp.AddScriptSrc().Self();
            csp.AddStyleSrc().Self().UnsafeInline();
            csp.AddImgSrc().Self().Data().From("https://lh3.googleusercontent.com");
            csp.AddConnectSrc().Self();
            csp.AddFrameAncestors().None();
            csp.AddFormAction().Self().From("https://accounts.google.com");
        })
        .AddPermissionsPolicy(permissions =>
        {
            permissions.AddAccelerometer().None();
            permissions.AddCamera().None();
            permissions.AddGeolocation().None();
            permissions.AddMicrophone().None();
            permissions.AddPayment().None();
            permissions.AddUsb().None();
        }));

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseRouting();
app.UseCors("KanbanWebApp");
app.UseRateLimiter();
// T050: app.UseAuthentication(); app.UseAuthorization();

// ── Health endpoints — public, excluded from auth group ───────────────────────

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

// ── OpenAPI — development only ────────────────────────────────────────────────

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// ── Versioned API (v1) — endpoints registered in Endpoints/ ───────────────────

var v1 = app.NewVersionedApi();
var v1Group = v1.MapGroup("/api/v1").HasApiVersion(1, 0);
// T050 adds: .RequireAuthorization("RegisteredUser")
// T051: AuthEndpoints.Map(v1Group);
// T060: InviteEndpoints.Map(v1Group);

app.Run();

public partial class Program { }
