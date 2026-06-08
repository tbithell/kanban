using Kanban.Contracts;
using Kanban.Domain.Entities;

namespace Kanban.Business.Transforms;

public static class CardTransforms
{
    public static CardDto ToDto(Card card) =>
        new(
            Id: card.Id,
            LaneId: card.LaneId,
            BoardId: card.BoardId,
            Title: card.Title,
            Description: card.Description,
            DueDate: card.DueDate,
            Position: card.Position,
            Version: card.Version,
            CreatedAt: card.CreatedAt,
            UpdatedAt: card.UpdatedAt);
}
