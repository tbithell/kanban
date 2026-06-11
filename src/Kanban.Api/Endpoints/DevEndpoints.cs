using System.Data;
using System.Security.Claims;
using Dapper;
using FluentValidation;
using FluentValidation.Results;
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

        routes.MapPost("/dev/seed-board-member",
            async (
                DevSeedBoardMemberRequest request,
                IDbConnection db) =>
            {
                var validRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Owner", "Member", "Viewer" };
                if (!validRoles.Contains(request.Role))
                    return Results.BadRequest($"Invalid role '{request.Role}'. Must be Owner, Member, or Viewer.");

                var now = DateTimeOffset.UtcNow;
                await db.ExecuteAsync(
                    """
                    INSERT OR IGNORE INTO users (id, google_sub, email, display_name, system_role, registered_at)
                    VALUES (@id, @sub, @email, @displayName, 'Standard', @createdAt)
                    """,
                    new
                    {
                        id = Guid.NewGuid().ToString("D"),
                        sub = $"dev-sub-{request.Email}",
                        email = request.Email,
                        displayName = request.Email,
                        createdAt = now.ToString("o"),
                    });

                var userId = await db.QuerySingleAsync<string>(
                    "SELECT id FROM users WHERE email = @email",
                    new { email = request.Email });

                var adminId = await db.QuerySingleOrDefaultAsync<string>(
                    "SELECT id FROM users WHERE system_role = 'Admin' LIMIT 1");
                if (adminId is null)
                    return Results.NotFound("No admin user found in database");

                var memberId = Guid.NewGuid();
                await db.ExecuteAsync(
                    """
                    INSERT OR IGNORE INTO board_members (id, board_id, user_id, role, invited_by_user_id, joined_at)
                    VALUES (@id, @boardId, @userId, @role, @invitedBy, @joinedAt)
                    """,
                    new
                    {
                        id = memberId.ToString("D"),
                        boardId = request.BoardId.ToString("D"),
                        userId = userId,
                        role = request.Role,
                        invitedBy = adminId,
                        joinedAt = now.ToString("o"),
                    });

                return Results.Ok(new { userId = Guid.Parse(userId) });
            })
            .WithName("DevSeedBoardMember")
            .WithSummary("[DEV ONLY] Seed a user and board membership — Playwright use only")
            .AllowAnonymous();

        routes.MapGet("/dev/test/throw-validation", () =>
            {
                var failures = new[] { new ValidationFailure("TestField", "Must not be empty") };
                throw new ValidationException("Validation failed", failures);
#pragma warning disable CS0162
                return Results.Ok();
#pragma warning restore CS0162
            })
            .WithName("DevTestThrowValidation")
            .WithSummary("[DEV ONLY] Throw ValidationException — error handler mapping test only")
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

internal sealed record DevSeedBoardMemberRequest(
    string Email,
    Guid BoardId,
    string Role);
