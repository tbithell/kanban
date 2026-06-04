using Kanban.Api.Auth;
using Kanban.Api.Options;
using Kanban.Business.Interfaces;
using Kanban.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using CorsOptions = Kanban.Api.Options.CorsOptions;

namespace Kanban.Api.Endpoints;

public static class AuthEndpoints
{
    private static string SafeRedirectUri(string? returnUrl, string frontendBase, string[] allowedOrigins)
    {
        if (string.IsNullOrWhiteSpace(returnUrl)) return frontendBase;
        if (returnUrl.StartsWith('/')) return $"{frontendBase}{returnUrl}";
        foreach (var origin in allowedOrigins)
            if (returnUrl.StartsWith(origin, StringComparison.OrdinalIgnoreCase))
                return returnUrl;
        return frontendBase;
    }

    public static void Map(IEndpointRouteBuilder routes)
    {
        var auth = routes.MapGroup("/auth");

        auth.MapGet("/signin",
            ([FromQuery] string? returnUrl, IOptions<CorsOptions> corsOptions) =>
            {
                var frontendBase = corsOptions.Value.AllowedOrigins.FirstOrDefault()
                    ?? "http://localhost:5173";
                var redirectUri = string.IsNullOrWhiteSpace(returnUrl)
                    ? frontendBase
                    : returnUrl.StartsWith('/')
                        ? $"{frontendBase}{returnUrl}"
                        : returnUrl;
                return Results.Challenge(
                    new AuthenticationProperties { RedirectUri = redirectUri },
                    [GoogleDefaults.AuthenticationScheme]);
            })
            .WithName("SignIn")
            .WithSummary("Initiate Google OAuth sign-in")
            .Produces(302)
            .AllowAnonymous()
            .RequireRateLimiting("anonymous");

        auth.MapGet("/me",
            async (ICurrentUserService currentUser, IAuthService authService, CancellationToken ct) =>
            {
                if (!currentUser.IsRegistered || !currentUser.UserId.HasValue)
                    return Results.Forbid();

                var dto = await authService.GetCurrentUserAsync(currentUser.UserId.Value, ct);
                return dto is null ? Results.Forbid() : Results.Ok(dto);
            })
            .WithName("GetCurrentUser")
            .WithSummary("Get the currently authenticated registered user")
            .Produces<CurrentUserDto>(200)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .RequireAuthorization("RegisteredUser");

        auth.MapPost("/signout",
            async (HttpContext ctx) =>
            {
                await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Results.NoContent();
            })
            .WithName("SignOut")
            .WithSummary("Sign out the current user")
            .Produces(204)
            .RequireAuthorization();
    }
}
