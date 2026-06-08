using Kanban.Business.Interfaces;
using Kanban.Contracts;

namespace Kanban.Api.Endpoints;

public static class LaneEndpoints
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        var lanes = routes.MapGroup("/boards/{boardId:guid}/lanes");

        lanes.MapPost("", async (Guid boardId, CreateLaneRequest request, ILaneService laneService) =>
        {
            var lane = await laneService.CreateAsync(boardId, request);
            return Results.Created($"/api/v1/boards/{boardId}/lanes/{lane.Id}", lane);
        })
        .WithName("CreateLane")
        .WithSummary("Create a lane on a board")
        .Produces<LaneDto>(201)
        .ProducesProblem(403)
        .ProducesProblem(404)
        .ProducesProblem(409)
        .ProducesProblem(422)
        .RequireRateLimiting("mutating");

        lanes.MapPatch("{laneId:guid}", async (Guid boardId, Guid laneId, RenameLaneRequest request, ILaneService laneService) =>
            Results.Ok(await laneService.RenameAsync(boardId, laneId, request)))
            .WithName("RenameLane")
            .WithSummary("Rename a lane")
            .Produces<LaneDto>(200)
            .ProducesProblem(403)
            .ProducesProblem(404)
            .ProducesProblem(409)
            .RequireRateLimiting("mutating");

        lanes.MapDelete("{laneId:guid}", async (Guid boardId, Guid laneId, ILaneService laneService) =>
        {
            await laneService.DeleteAsync(boardId, laneId);
            return Results.NoContent();
        })
        .WithName("DeleteLane")
        .WithSummary("Delete a lane")
        .Produces(204)
        .ProducesProblem(403)
        .ProducesProblem(404)
        .RequireRateLimiting("mutating");

        lanes.MapPost("{laneId:guid}/move", async (Guid boardId, Guid laneId, MoveLaneRequest request, ILaneService laneService) =>
        {
            await laneService.MoveAsync(boardId, laneId, request);
            return Results.Ok();
        })
        .WithName("MoveLane")
        .WithSummary("Move a lane to a new position")
        .Produces(200)
        .ProducesProblem(403)
        .ProducesProblem(404)
        .ProducesProblem(409)
        .RequireRateLimiting("mutating");
    }
}
