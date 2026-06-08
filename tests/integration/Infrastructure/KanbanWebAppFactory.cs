using System.Collections.Concurrent;
using System.Data;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Dapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kanban.Tests.Integration.Infrastructure;

public sealed class KanbanWebAppFactory : WebApplicationFactory<Program>
{
    // Path.GetTempFileName() creates an existing 0-byte file; DbUp skips migrations when the
    // target file already exists. Path.GetRandomFileName() returns a name only — no file is
    // created — so DbUp initialises a fresh SQLite database on startup.
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public static readonly string AdminEmail = "admin@test.local";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Development disables HTTPS redirect and is needed for the app to start correctly in tests.
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Google:ClientId"] = "test-client-id",
                ["Authentication:Google:ClientSecret"] = "test-client-secret",
                ["ConnectionStrings:Kanban"] = $"Data Source={_dbPath}",
                ["Cors:AllowedOrigins:0"] = "http://localhost:5173",
                ["Seed:AdminEmail"] = AdminEmail,
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace all authentication with the Test scheme so requests can be
            // authenticated by setting a header without going through Google OAuth.
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = TestAuthenticationHandler.SchemeName;
                options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
                options.DefaultForbidScheme = TestAuthenticationHandler.SchemeName;
            })
            .AddScheme<TestAuthHandlerOptions, TestAuthenticationHandler>(
                TestAuthenticationHandler.SchemeName, null);

            // Replace production rate limits with permissive values so concurrency tests
            // are not interfered with by the rate limiter.
            // MAINTENANCE: this block must enumerate every named policy defined in Program.cs.
            // If a new policy is added there, add it here too — a missing policy causes a
            // runtime error on any endpoint tagged with .RequireRateLimiting("new-policy").
            services.RemoveAll(typeof(IConfigureOptions<RateLimiterOptions>));
            services.Configure<RateLimiterOptions>(opts =>
            {
                opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                opts.AddFixedWindowLimiter("anonymous", o =>
                {
                    o.PermitLimit = 10_000;
                    o.Window = TimeSpan.FromMinutes(1);
                });
                opts.AddSlidingWindowLimiter("authenticated", o =>
                {
                    o.PermitLimit = 10_000;
                    o.Window = TimeSpan.FromMinutes(1);
                    o.SegmentsPerWindow = 6;
                });
                opts.AddSlidingWindowLimiter("mutating", o =>
                {
                    o.PermitLimit = 10_000;
                    o.Window = TimeSpan.FromMinutes(1);
                    o.SegmentsPerWindow = 6;
                });
            });
        });
    }

    public async Task<string> InsertInvitationWithRawTokenAsync(
        string email, Guid issuedByUserId,
        DateTimeOffset? expiresAt = null, DateTimeOffset? consumedAt = null)
    {
        _ = Server;
        var (rawToken, tokenHash) = GenerateRawToken();
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDbConnection>();
        var id = Guid.NewGuid();
        await db.ExecuteAsync(
            """
            INSERT INTO invitations (id, email, issued_by_user_id, token_hash, issued_at, expires_at,
                                     consumed_at, consumed_by_user_id)
            VALUES (@id, @email, @issuedByUserId, @tokenHash, @issuedAt, @expiresAt, @consumedAt, NULL)
            """,
            new
            {
                id = id.ToString("D"),
                email,
                issuedByUserId = issuedByUserId.ToString("D"),
                tokenHash,
                issuedAt = DateTimeOffset.UtcNow.ToString("o"),
                expiresAt = (expiresAt ?? DateTimeOffset.UtcNow.AddDays(7)).ToString("o"),
                consumedAt = consumedAt?.ToString("o"),
            });
        return rawToken;
    }

    public async Task<int> CountUsersForEmailAsync(string email)
    {
        _ = Server;
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDbConnection>();
        return await db.QuerySingleAsync<int>(
            "SELECT COUNT(*) FROM users WHERE email = @email", new { email });
    }

    private static (string rawToken, string tokenHash) GenerateRawToken()
    {
        var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
        var raw = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var hash = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(raw))).ToLower();
        return (raw, hash);
    }

    public async Task InsertActiveInvitationAsync(string email, Guid issuedByUserId)
    {
        _ = Server;
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDbConnection>();
        var id = Guid.NewGuid();
        var tokenHash = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)));
        await db.ExecuteAsync(
            """
            INSERT INTO invitations (id, email, issued_by_user_id, token_hash, issued_at, expires_at,
                                     consumed_at, consumed_by_user_id)
            VALUES (@id, @email, @issuedByUserId, @tokenHash, @issuedAt, @expiresAt, NULL, NULL)
            """,
            new
            {
                id = id.ToString("D"),
                email,
                issuedByUserId = issuedByUserId.ToString("D"),
                tokenHash,
                issuedAt = DateTimeOffset.UtcNow.ToString("o"),
                expiresAt = DateTimeOffset.UtcNow.AddDays(7).ToString("o"),
            });
    }

    public async Task<Guid> GetSeededAdminIdAsync()
    {
        _ = Server; // ensure app startup (and DbUp migrations) have run before querying
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDbConnection>();
        return await db.QuerySingleAsync<Guid>(
            "SELECT id FROM users WHERE email = @email",
            new { email = AdminEmail });
    }

    public HttpClient CreateAuthenticatedClient(ClaimsPrincipal user)
    {
        var client = CreateClient();
        var token = TestAuthenticationHandler.RegisterUser(user);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthenticationHandler.SchemeName, token);
        return client;
    }

    public async Task<Guid> InsertStandardUserAsync(string email)
    {
        _ = Server;
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDbConnection>();
        var userId = Guid.NewGuid();
        await db.ExecuteAsync(
            """
            INSERT INTO users (id, email, display_name, system_role, google_sub, registered_at)
            VALUES (@id, @email, @displayName, 'Standard', @googleSub, @registeredAt)
            """,
            new
            {
                id = userId.ToString("D"),
                email,
                displayName = email,
                googleSub = $"google-sub-{userId:N}",
                registeredAt = DateTimeOffset.UtcNow.ToString("o"),
            });
        return userId;
    }

    public async Task<Guid> InsertBoardAsync(string name, Guid ownerUserId)
    {
        _ = Server;
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDbConnection>();
        var boardId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        await db.ExecuteAsync(
            """
            INSERT INTO boards (id, name, created_by_user_id, created_at)
            VALUES (@id, @name, @createdByUserId, @createdAt)
            """,
            new
            {
                id = boardId.ToString("D"),
                name,
                createdByUserId = ownerUserId.ToString("D"),
                createdAt = now.ToString("o"),
            });
        var memberId = Guid.NewGuid();
        await db.ExecuteAsync(
            """
            INSERT INTO board_members (id, board_id, user_id, role, invited_by_user_id, joined_at)
            VALUES (@id, @boardId, @userId, 'Owner', NULL, @joinedAt)
            """,
            new
            {
                id = memberId.ToString("D"),
                boardId = boardId.ToString("D"),
                userId = ownerUserId.ToString("D"),
                joinedAt = now.ToString("o"),
            });
        return boardId;
    }

    public async Task InsertBoardMemberAsync(Guid boardId, Guid userId, string role)
    {
        _ = Server;
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDbConnection>();
        var memberId = Guid.NewGuid();
        var adminId = await GetSeededAdminIdAsync();
        await db.ExecuteAsync(
            """
            INSERT INTO board_members (id, board_id, user_id, role, invited_by_user_id, joined_at)
            VALUES (@id, @boardId, @userId, @role, @invitedBy, @joinedAt)
            """,
            new
            {
                id = memberId.ToString("D"),
                boardId = boardId.ToString("D"),
                userId = userId.ToString("D"),
                role,
                invitedBy = adminId.ToString("D"),
                joinedAt = DateTimeOffset.UtcNow.ToString("o"),
            });
    }

    public async Task<Guid> InsertLaneAsync(Guid boardId, string name, int position)
    {
        _ = Server;
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDbConnection>();
        var laneId = Guid.NewGuid();
        await db.ExecuteAsync(
            """
            INSERT INTO lanes (id, board_id, name, position, version, created_at)
            VALUES (@id, @boardId, @name, @position, 1, @createdAt)
            """,
            new
            {
                id = laneId.ToString("D"),
                boardId = boardId.ToString("D"),
                name,
                position,
                createdAt = DateTimeOffset.UtcNow.ToString("o"),
            });
        return laneId;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && File.Exists(_dbPath))
            File.Delete(_dbPath);
    }
}

public sealed class TestAuthHandlerOptions : AuthenticationSchemeOptions { }

public sealed class TestAuthenticationHandler(
    IOptionsMonitor<TestAuthHandlerOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<TestAuthHandlerOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";

    // ConcurrentDictionary is used instead of [ThreadStatic] because async request
    // processing can switch threads, making thread-local storage unreliable in tests.
    private static readonly ConcurrentDictionary<string, ClaimsPrincipal> _users = new();

    public static string RegisterUser(ClaimsPrincipal user)
    {
        var token = Guid.NewGuid().ToString("N");
        _users[token] = user;
        return token;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
            return Task.FromResult(AuthenticateResult.NoResult());

        var headerValue = authHeader.ToString();
        var prefix = $"{SchemeName} ";
        if (!headerValue.StartsWith(prefix, StringComparison.Ordinal))
            return Task.FromResult(AuthenticateResult.NoResult());

        var token = headerValue[prefix.Length..];
        if (!_users.TryGetValue(token, out var user))
            return Task.FromResult(AuthenticateResult.NoResult());

        var ticket = new AuthenticationTicket(user, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public static class TestPrincipals
{
    public static ClaimsPrincipal RegisteredAdmin(Guid userId) =>
        new(new ClaimsIdentity(
            [
                new Claim("sub", "admin-google-sub"),
                new Claim("email", KanbanWebAppFactory.AdminEmail),
                new Claim("user_id", userId.ToString()),
                new Claim("system_role", "admin"),
            ],
            TestAuthenticationHandler.SchemeName));

    public static ClaimsPrincipal RegisteredStandardUser(Guid userId, string email) =>
        new(new ClaimsIdentity(
            [
                new Claim("sub", $"google-sub-{userId:N}"),
                new Claim("email", email),
                new Claim("user_id", userId.ToString()),
                new Claim("system_role", "standard"),
            ],
            TestAuthenticationHandler.SchemeName));

    public static ClaimsPrincipal UnregisteredGoogleUser() =>
        UnregisteredGoogleUser("unregistered@example.com");

    public static ClaimsPrincipal UnregisteredGoogleUser(
        string email,
        string? googleSub = null,
        string displayName = "Test Invitee") =>
        new(new ClaimsIdentity(
            [
                new Claim("sub", googleSub ?? $"google-sub-{email}"),
                new Claim("email", email),
                new Claim("name", displayName),
            ],
            TestAuthenticationHandler.SchemeName));
}
