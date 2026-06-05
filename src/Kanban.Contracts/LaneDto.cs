namespace Kanban.Contracts;

public sealed record LaneDto(
    Guid Id,
    Guid BoardId,
    string Name,
    int Position,
    int Version,
    IReadOnlyList<CardDto> Cards
);
