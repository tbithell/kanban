namespace Kanban.Contracts;

public sealed record BoardDetailDto(
    Guid Id,
    string Name,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    BoardRoleDto CallerRole,
    IReadOnlyList<LaneDto> Lanes
);
