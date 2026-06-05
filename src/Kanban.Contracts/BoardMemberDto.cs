namespace Kanban.Contracts;

public sealed record BoardMemberDto(
    Guid UserId,
    string DisplayName,
    BoardRoleDto Role,
    DateTimeOffset JoinedAt
);
