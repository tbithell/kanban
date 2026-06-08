using Kanban.Business.Interfaces;
using Kanban.Contracts;
using Kanban.DataAccess.Interfaces;
using Kanban.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace Kanban.Api.Auth;

public sealed class BoardAuthorizationHandler(
    IBoardMemberRepository boardMemberRepository,
    ICurrentUserService currentUserService)
    : AuthorizationHandler<BoardMembershipRequirement, BoardContext>
{
    private readonly IBoardMemberRepository _boardMemberRepository = boardMemberRepository;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        BoardMembershipRequirement requirement,
        BoardContext resource)
    {
        if (!_currentUserService.IsRegistered || !_currentUserService.UserId.HasValue)
            return;

        var role = await _boardMemberRepository.FindRoleAsync(
            resource.BoardId, _currentUserService.UserId.Value);

        if (role is null)
            return;

        if (MeetsRequirement(requirement.Operation, role.Value))
            context.Succeed(requirement);
    }

    private static bool MeetsRequirement(string operation, BoardRole role) => operation switch
    {
        BoardOperations.Read          => role is BoardRole.Owner or BoardRole.Member or BoardRole.Viewer,
        BoardOperations.CreateCard    => role is BoardRole.Owner or BoardRole.Member,
        BoardOperations.UpdateCard    => role is BoardRole.Owner or BoardRole.Member,
        BoardOperations.DeleteCard    => role is BoardRole.Owner or BoardRole.Member,
        BoardOperations.CreateLane    => role is BoardRole.Owner or BoardRole.Member,
        BoardOperations.UpdateLane    => role is BoardRole.Owner or BoardRole.Member,
        BoardOperations.DeleteLane    => role is BoardRole.Owner or BoardRole.Member,
        BoardOperations.ManageMembers => role is BoardRole.Owner,
        BoardOperations.DeleteBoard   => role is BoardRole.Owner,
        _                             => false,
    };
}
