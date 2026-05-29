using System.Security.Claims;
using FluentValidation;
using Kanban.Api.Auth;
using Microsoft.AspNetCore.Authentication;
using Kanban.Business.Interfaces;
using Kanban.Business.Transforms;
using Kanban.Contracts;
using Kanban.Domain.Enums;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using CorsOptions = Kanban.Api.Options.CorsOptions;

namespace Kanban.Api.Endpoints;

public static class InviteEndpoints
{
    public static void MapAcceptEndpoint(IEndpointRouteBuilder routes)
    {
        routes.MapPost("/invites/{token}/accept",
            async (
                string token,
                HttpContext ctx,
                IInvitationService invitationService,
                CancellationToken ct) =>
            {
                var googleEmail = ctx.User.FindFirst("email")?.Value;
                var googleSub = ctx.User.FindFirst("sub")?.Value;
                var displayName = ctx.User.FindFirst("name")?.Value ?? googleEmail;

                if (string.IsNullOrEmpty(googleEmail) || string.IsNullOrEmpty(googleSub))
                    return Results.Unauthorized();

                var user = await invitationService.AcceptAsync(
                    token, googleEmail, googleSub, displayName!, ct);

                var identity = new ClaimsIdentity(
                    [
                        new Claim("sub", googleSub),
                        new Claim("email", user.Email),
                        new Claim("user_id", user.Id.ToString()),
                        new Claim("system_role", "standard"),
                    ],
                    CookieAuthenticationDefaults.AuthenticationScheme);
                await ctx.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(identity));

                return Results.Ok(UserTransforms.ToDto(user));
            })
            .WithName("AcceptInvite")
            .WithSummary("Accept an invitation and become a registered user")
            .Produces<CurrentUserDto>(200)
            .ProducesProblem(410)
            .ProducesProblem(422)
            .RequireAuthorization()
            .RequireRateLimiting("anonymous");
    }

    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapPost("/invites",
            async (
                [FromBody] IssueInviteRequest request,
                IValidator<IssueInviteRequest> validator,
                IInvitationService invitationService,
                ICurrentUserService currentUser,
                IOptions<CorsOptions> corsOptions,
                CancellationToken ct) =>
            {
                var validationResult = await validator.ValidateAsync(request, ct);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(e => e.PropertyName.ToLowerInvariant())
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                    return Results.Problem(
                        title: "Validation failed.",
                        statusCode: StatusCodes.Status422UnprocessableEntity,
                        extensions: new Dictionary<string, object?>
                        {
                            ["code"] = "validation.failed",
                            ["errors"] = errors,
                        });
                }

                if (!currentUser.UserId.HasValue)
                    return Results.Forbid();

                var callerRole = currentUser.SystemRole == "admin"
                    ? SystemRole.Admin
                    : SystemRole.Standard;

                var frontendBaseUrl = corsOptions.Value.AllowedOrigins.FirstOrDefault()
                    ?? throw new InvalidOperationException(
                        "Cors:AllowedOrigins must contain at least one entry.");

                var (response, isNew) = await invitationService.IssueAsync(
                    request.Email,
                    currentUser.UserId.Value,
                    callerRole,
                    frontendBaseUrl,
                    ct);

                return isNew
                    ? Results.Created((string?)null, response)
                    : Results.Ok(response);
            })
            .WithName("IssueInvite")
            .WithSummary("Issue an invitation to a new user")
            .Produces<IssueInviteResponse>(201)
            .Produces<IssueInviteResponse>(200)
            .ProducesProblem(422)
            .ProducesProblem(409)
            .ProducesProblem(403)
            .RequireAuthorization("Admin")
            .RequireRateLimiting("mutating");
    }
}
