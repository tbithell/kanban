namespace Kanban.Contracts;

public sealed record CardDto(
    Guid Id,
    Guid LaneId,
    Guid BoardId,
    string Title,
    string? Description,
    DateOnly? DueDate,
    int Position,
    int Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);
