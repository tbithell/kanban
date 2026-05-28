using Kanban.Api.Auth;
using Kanban.Business.Interfaces;
using Kanban.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;

namespace Kanban.Api.Endpoints;

public static class AuthEndpoints
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        var auth = routes.MapGroup("/auth");

        auth.MapGet("/signin",
            ([FromQuery] string? returnUrl) =>
            {
                var redirectUri = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
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
