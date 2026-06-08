using System.Diagnostics;
using Kanban.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Kanban.Api.Auth;

public sealed class RegisteredUserHandler(ICurrentUserService currentUserService)
    : AuthorizationHandler<RegisteredUserRequirement>
{
    private readonly ICurrentUserService _currentUserService = currentUserService;

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RegisteredUserRequirement requirement)
    {
        if (_currentUserService.IsRegistered)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail(new AuthorizationFailureReason(
                this, "user.not_registered"));
        }

        return Task.CompletedTask;
    }
}
