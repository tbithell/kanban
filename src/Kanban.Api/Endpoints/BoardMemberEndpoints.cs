using Kanban.Api.Options;
using Kanban.Business.Interfaces;
using Kanban.Contracts;
using Microsoft.Extensions.Options;

namespace Kanban.Api.Endpoints;

public static class BoardMemberEndpoints
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        var members = routes.MapGroup("/boards/{boardId:guid}/members");

        members.MapGet("",
            async (Guid boardId, IBoardMembershipService membershipService) =>
                Results.Ok(await membershipService.ListMembersAsync(boardId)))
            .WithName("GetBoardMembers")
            .WithSummary("List members of a board")
            .Produces<IReadOnlyList<BoardMemberDto>>(200)
            .ProducesProblem(404)
            .RequireRateLimiting("authenticated");

        members.MapPost("/invite",
            async (
                Guid boardId,
                InviteBoardMemberRequest request,
                IBoardMembershipService membershipService,
                IOptions<CorsOptions> corsOptions) =>
            {
                var frontendBaseUrl = corsOptions.Value.AllowedOrigins.FirstOrDefault()
                    ?? "http://localhost:5173";
                await membershipService.InviteAsync(boardId, request, frontendBaseUrl);
                return Results.Accepted();
            })
            .WithName("InviteBoardMember")
            .WithSummary("Invite a user to a board (Owner only)")
            .Produces(202)
            .ProducesProblem(403)
            .ProducesProblem(404)
            .ProducesProblem(409)
            .ProducesProblem(422)
            .RequireRateLimiting("mutating");

        members.MapPatch("{userId:guid}/role",
            async (
                Guid boardId,
                Guid userId,
                ChangeMemberRoleRequest request,
                IBoardMembershipService membershipService) =>
                Results.Ok(await membershipService.ChangeRoleAsync(boardId, userId, request)))
            .WithName("ChangeMemberRole")
            .WithSummary("Change a member's board role (Owner only)")
            .Produces<BoardMemberDto>(200)
            .ProducesProblem(403)
            .ProducesProblem(404)
            .ProducesProblem(422)
            .RequireRateLimiting("mutating");

        members.MapDelete("{userId:guid}",
            async (
                Guid boardId,
                Guid userId,
                IBoardMembershipService membershipService) =>
            {
                await membershipService.RemoveMemberAsync(boardId, userId);
                return Results.NoContent();
            })
            .WithName("RemoveBoardMember")
            .WithSummary("Remove a member from a board (Owner only)")
            .Produces(204)
            .ProducesProblem(403)
            .ProducesProblem(404)
            .ProducesProblem(422)
            .RequireRateLimiting("mutating");
    }
}
