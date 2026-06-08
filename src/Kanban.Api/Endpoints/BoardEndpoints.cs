using Kanban.Business.Interfaces;
using Kanban.Contracts;

namespace Kanban.Api.Endpoints;

public static class BoardEndpoints
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        var boards = routes.MapGroup("/boards");

        boards.MapGet("", async (IBoardService boardService) =>
            Results.Ok(await boardService.ListForCallerAsync()))
            .WithName("GetBoards")
            .WithSummary("List all boards for the current user")
            .Produces<IReadOnlyList<BoardSummaryDto>>(200)
            .ProducesProblem(401)
            .RequireRateLimiting("authenticated");

        boards.MapPost("", async (CreateBoardRequest request, IBoardService boardService) =>
        {
            var board = await boardService.CreateAsync(request);
            return Results.Created($"/api/v1/boards/{board.Id}", board);
        })
        .WithName("CreateBoard")
        .WithSummary("Create a new board (Admin only)")
        .Produces<BoardDetailDto>(201)
        .ProducesProblem(403)
        .ProducesProblem(409)
        .ProducesProblem(422)
        .RequireRateLimiting("mutating");

        boards.MapGet("{boardId:guid}", async (Guid boardId, IBoardService boardService) =>
            Results.Ok(await boardService.GetAsync(boardId)))
            .WithName("GetBoard")
            .WithSummary("Get a board by ID")
            .Produces<BoardDetailDto>(200)
            .ProducesProblem(404)
            .RequireRateLimiting("authenticated");

        boards.MapDelete("{boardId:guid}", async (Guid boardId, IBoardService boardService) =>
        {
            await boardService.DeleteAsync(boardId);
            return Results.NoContent();
        })
        .WithName("DeleteBoard")
        .WithSummary("Delete a board (Owner or Admin only)")
        .Produces(204)
        .ProducesProblem(403)
        .ProducesProblem(404)
        .RequireRateLimiting("mutating");
    }
}
