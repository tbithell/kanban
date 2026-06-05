using Kanban.Contracts;
using Kanban.Domain.Entities;

namespace Kanban.Business.Transforms;

public static class LaneTransforms
{
    public static LaneDto ToDto(Lane lane, IReadOnlyList<CardDto> cards) =>
        new(
            Id: lane.Id,
            BoardId: lane.BoardId,
            Name: lane.Name,
            Position: lane.Position,
            Version: lane.Version,
            Cards: cards);
}
