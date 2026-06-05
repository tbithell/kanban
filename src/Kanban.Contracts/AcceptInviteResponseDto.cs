namespace Kanban.Contracts;

public sealed record AcceptInviteResponseDto(
    CurrentUserDto User,
    Guid? BoardId
);
