namespace Kanban.Contracts;

public sealed record BoardSummaryDto(
    Guid Id,
    string Name,
    int LaneCount,
    int CardCount,
    BoardRoleDto CallerRole
);
