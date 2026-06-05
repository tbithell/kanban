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
        Verify.That(id).IsNotDefault();
        Verify.That(boardId).IsNotDefault();
        Verify.That(userId).IsNotDefault();
        Id = id;
        BoardId = boardId;
        UserId = userId;
        Role = role;
        InvitedByUserId = invitedByUserId;
        JoinedAt = joinedAt;
    }

    public void ChangeRole(BoardRole newRole) => Role = newRole;
}
