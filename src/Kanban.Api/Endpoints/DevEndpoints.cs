using System.Data;
using System.Security.Claims;
using Dapper;
using Kanban.Api.Options;
using Kanban.DataAccess.Interfaces;
using Kanban.Domain.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace Kanban.Api.Endpoints;

// Dev-only test infrastructure — architecture boundary exception.
// Direct DataAccess and Domain usage is intentional here; this file is
// never registered outside IsDevelopment() and must not ship to production.
internal static class DevEndpoints
{
    internal static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapGet("/dev/authenticate",
            async (
                string email,
                string? displayName,
                IUserRepository userRepository,
                IOptions<CorsOptions> corsOptions,
                HttpContext ctx) =>
            {
                var user = await userRepository.FindByEmailAsync(email);

                List<Claim> claims;
                if (user is not null)
                {
                    claims =
                    [
                        new Claim("sub", user.GoogleSub ?? $"dev-sub-{email}"),
                        new Claim("email", user.Email),
                        new Claim("name", user.DisplayName),
                        new Claim("user_id", user.Id.ToString()),
                        new Claim("system_role",
                            user.SystemRole == SystemRole.Admin ? "admin" : "standard"),
                    ];
                }
                else
                {
                    claims =
                    [
                        new Claim("sub", $"dev-sub-{email}"),
                        new Claim("email", email),
                        new Claim("name", displayName ?? email),
                    ];
                }

                var identity = new ClaimsIdentity(
                    claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await ctx.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(identity));

                var frontendOrigin = corsOptions.Value.AllowedOrigins.FirstOrDefault()
                    ?? "http://localhost:5173";
                return Results.Redirect(frontendOrigin);
            })
            .WithName("DevAuthenticate")
            .WithSummary("[DEV ONLY] Issue an auth cookie without Google OAuth — Playwright use only")
            .AllowAnonymous();

        routes.MapPost("/dev/seed/invitation",
            async (
                DevSeedInvitationRequest request,
                IDbConnection db) =>
            {
                var adminId = await db.QuerySingleOrDefaultAsync<string>(
                    "SELECT id FROM users WHERE system_role = 'Admin' LIMIT 1");

                if (adminId is null)
                    return Results.NotFound("No admin user found in database");

                var (rawToken, tokenHash) = GenerateToken();
                var now = DateTimeOffset.UtcNow;
                var expiresAt = now.AddDays(request.ExpiresInDays ?? 7);
                var consumedAt = request.Consumed ? now.AddMinutes(-1) : (DateTimeOffset?)null;

                await db.ExecuteAsync(
                    """
                    INSERT INTO invitations
                        (id, email, issued_by_user_id, token_hash, issued_at, expires_at, consumed_at, consumed_by_user_id)
                    VALUES (@id, @email, @issuedByUserId, @tokenHash, @issuedAt, @expiresAt, @consumedAt, NULL)
                    """,
                    new
                    {
                        id = Guid.NewGuid().ToString("D"),
                        email = request.Email,
                        issuedByUserId = adminId,
                        tokenHash,
                        issuedAt = now.ToString("o"),
                        expiresAt = expiresAt.ToString("o"),
                        consumedAt = consumedAt?.ToString("o"),
                    });

                return Results.Ok(new { token = rawToken });
            })
            .WithName("DevSeedInvitation")
            .WithSummary("[DEV ONLY] Seed an invitation record for Playwright testing")
            .AllowAnonymous();
    }

    private static (string rawToken, string tokenHash) GenerateToken()
    {
        var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
        var raw = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var hash = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(raw))).ToLower();
        return (raw, hash);
    }
}

internal sealed record DevSeedInvitationRequest(
    string Email,
    double? ExpiresInDays = null,
    bool Consumed = false);
