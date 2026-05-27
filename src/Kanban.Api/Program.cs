using System.Data;
using System.Diagnostics;
using System.Reflection;
using Asp.Versioning;
using DbUp;
using DbUp.Sqlite;
using Kanban.AntiCorruption.Adapters;
using Kanban.Api.Auth;
using Kanban.Api.ErrorHandling;
using Kanban.Api.Health;
using Kanban.Api.Options;
using Kanban.Business.Interfaces;
using Kanban.Business.Services;
using Kanban.Api.Endpoints;
using Kanban.DataAccess;
using Kanban.DataAccess.Interfaces;
using Kanban.DataAccess.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
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

// Resolve the connection string lazily from IOptions so that WebApplicationFactory
// configuration overrides (applied after builder services are registered) are honoured.
builder.Services.AddScoped<IDbConnection>(sp =>
{
    var opts = sp.GetRequiredService<IOptions<ConnectionStringOptions>>().Value;
    var conn = new SqliteConnection(opts.Kanban);
    conn.Open();
    return conn;
});

// ── Exception Handling ────────────────────────────────────────────────────────

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        ctx.ProblemDetails.Extensions["traceId"] = Activity.Current?.Id;
        if (ctx.HttpContext.Response.StatusCode == StatusCodes.Status403Forbidden
            && !ctx.ProblemDetails.Extensions.ContainsKey("code"))
        {
            ctx.ProblemDetails.Title = "You are not registered to use this application.";
            ctx.ProblemDetails.Extensions["code"] = "user.not_registered";
        }
    };
});
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

// ── Authentication & Authorization ───────────────────────────────────────────

builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<GoogleIdentityAdapter>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthEventRepository, AuthEventRepository>();
builder.Services.AddScoped<IInvitationRepository, InvitationRepository>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuthorizationHandler, RegisteredUserHandler>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/api/v1/auth/signin";
        options.Events.OnRedirectToLogin = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    })
    .AddGoogle(options =>
    {
        var googleOpts = builder.Configuration
            .GetSection(GoogleAuthOptions.SectionName)
            .Get<GoogleAuthOptions>()!;
        options.ClientId = googleOpts.ClientId;
        options.ClientSecret = googleOpts.ClientSecret;
        options.Events.OnTicketReceived = async ctx =>
        {
            var adapter = ctx.HttpContext.RequestServices
                .GetRequiredService<GoogleIdentityAdapter>();
            var authService = ctx.HttpContext.RequestServices
                .GetRequiredService<IAuthService>();

            if (ctx.Principal is not null)
            {
                var identity = adapter.Extract(ctx.Principal);
                var claimsIdentity = ctx.Principal.Identity as System.Security.Claims.ClaimsIdentity;
                if (claimsIdentity is not null)
                {
                    await authService.HandleSignInAsync(
                        identity.Sub,
                        identity.Email,
                        claimsIdentity,
                        ctx.HttpContext.RequestAborted);
                }
            }
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RegisteredUser", policy => policy
        .RequireAuthenticatedUser()
        .AddRequirements(new RegisteredUserRequirement()))
    .AddPolicy("Admin", policy => policy
        .RequireAuthenticatedUser()
        .RequireClaim("system_role", "admin"));

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

// Read connection string post-build so WebApplicationFactory test overrides are resolved.
var connectionString = app.Configuration.GetConnectionString("Kanban")
    ?? throw new InvalidOperationException("ConnectionStrings:Kanban is required.");

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
app.UseAuthentication();
app.UseAuthorization();

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
var v1Group = v1.MapGroup("/api/v1")
    .HasApiVersion(1, 0)
    .RequireAuthorization("RegisteredUser");
AuthEndpoints.Map(v1Group);
// T060: InviteEndpoints.Map(v1Group);

app.Run();

public partial class Program { }
