using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
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
    private readonly string _dbPath = Path.GetTempFileName();

    public static readonly string AdminEmail = "admin@test.local";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

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
            services.AddAuthentication(TestAuthenticationHandler.SchemeName)
                .AddScheme<TestAuthHandlerOptions, TestAuthenticationHandler>(
                    TestAuthenticationHandler.SchemeName, null);
        });
    }

    public HttpClient CreateAuthenticatedClient(ClaimsPrincipal user)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthenticationHandler.SchemeName, user.Identity?.Name);
        TestAuthenticationHandler.SetUser(user);
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && File.Exists(_dbPath))
            File.Delete(_dbPath);
    }
}

public sealed class TestHandlerOptions : AuthenticationSchemeOptions { }

public sealed class TestAuthHandlerOptions : AuthenticationSchemeOptions { }

public sealed class TestAuthenticationHandler(
    IOptionsMonitor<TestAuthHandlerOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<TestAuthHandlerOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";

    [ThreadStatic]
    private static ClaimsPrincipal? _currentUser;

    public static void SetUser(ClaimsPrincipal user) => _currentUser = user;
    public static void ClearUser() => _currentUser = null;

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (_currentUser == null)
            return Task.FromResult(AuthenticateResult.NoResult());

        var ticket = new AuthenticationTicket(_currentUser, SchemeName);
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
