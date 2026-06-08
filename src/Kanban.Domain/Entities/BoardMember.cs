using FluentValidation;
using Kanban.Domain.Enums;

namespace Kanban.Domain.Entities;

public sealed class BoardMember
{
    public Guid Id { get; }
    public Guid BoardId { get; }
    public Guid UserId { get; }
    public BoardRole Role { get; private set; }
    public Guid? InvitedByUserId { get; }
    public DateTimeOffset JoinedAt { get; }

    public BoardMember(Guid id, Guid boardId, Guid userId, BoardRole role,
                       Guid? invitedByUserId, DateTimeOffset joinedAt)
    {
        new InlineValidator<Guid> { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("id") }.ValidateAndThrow(id);
        new InlineValidator<Guid> { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("boardId") }.ValidateAndThrow(boardId);
        new InlineValidator<Guid> { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("userId") }.ValidateAndThrow(userId);
        Id = id;
        BoardId = boardId;
        UserId = userId;
        Role = role;
        InvitedByUserId = invitedByUserId;
        JoinedAt = joinedAt;
    }

    public void ChangeRole(BoardRole newRole) => Role = newRole;
}
