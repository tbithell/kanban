using System.Collections.Concurrent;
using System.Data;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Dapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        new(new ClaimsIdentity(
            [
                new Claim("sub", "unregistered-google-sub"),
                new Claim("email", "unregistered@example.com"),
            ],
            TestAuthenticationHandler.SchemeName));
}
