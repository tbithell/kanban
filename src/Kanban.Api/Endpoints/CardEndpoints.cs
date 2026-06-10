using Kanban.Business.Interfaces;
using Kanban.Contracts;

namespace Kanban.Api.Endpoints;

public static class CardEndpoints
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        var cards = routes.MapGroup("/boards/{boardId:guid}");

        cards.MapPost("lanes/{laneId:guid}/cards",
            async (Guid boardId, Guid laneId, CreateCardRequest request, ICardService cardService) =>
            {
                var card = await cardService.CreateAsync(boardId, laneId, request);
                return Results.Created($"/api/v1/boards/{boardId}/cards/{card.Id}", card);
            })
            .WithName("CreateCard")
            .WithSummary("Create a card in a lane")
            .Produces<CardDto>(201)
            .ProducesProblem(403)
            .ProducesProblem(404)
            .ProducesProblem(422)
            .RequireRateLimiting("mutating");

        cards.MapPatch("cards/{cardId:guid}",
            async (Guid boardId, Guid cardId, UpdateCardRequest request, ICardService cardService) =>
                Results.Ok(await cardService.UpdateAsync(boardId, cardId, request)))
            .WithName("UpdateCard")
            .WithSummary("Update a card's fields")
            .Produces<CardDto>(200)
            .ProducesProblem(403)
            .ProducesProblem(404)
            .ProducesProblem(422)
            .RequireRateLimiting("mutating");

        cards.MapDelete("cards/{cardId:guid}",
            async (Guid boardId, Guid cardId, ICardService cardService) =>
            {
                await cardService.DeleteAsync(boardId, cardId);
                return Results.NoContent();
            })
            .WithName("DeleteCard")
            .WithSummary("Delete a card")
            .Produces(204)
            .ProducesProblem(403)
            .ProducesProblem(404)
            .RequireRateLimiting("mutating");

        cards.MapPost("cards/{cardId:guid}/move",
            async (Guid boardId, Guid cardId, MoveCardRequest request, ICardService cardService) =>
                Results.Ok(await cardService.MoveAsync(boardId, cardId, request)))
            .WithName("MoveCard")
            .WithSummary("Move a card to a new lane/position")
            .Produces<CardDto>(200)
            .ProducesProblem(403)
            .ProducesProblem(404)
            .ProducesProblem(409)
            .RequireRateLimiting("mutating");
    }
}
