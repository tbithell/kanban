using Kanban.Domain.Entities;
using Kanban.Domain.Enums;

namespace Kanban.Tests.Unit.Builders;

public sealed class BoardMemberBuilder
{
    private Guid _id = Guid.NewGuid();
    private Guid _boardId = Guid.NewGuid();
    private Guid _userId = Guid.NewGuid();
    private BoardRole _role = BoardRole.Member;
    private Guid? _invitedByUserId;
    private DateTimeOffset _joinedAt = DateTimeOffset.UtcNow;

    public static BoardMemberBuilder ABoardMember() => new();

    public BoardMemberBuilder WithId(Guid id) { _id = id; return this; }
    public BoardMemberBuilder OnBoard(Guid boardId) { _boardId = boardId; return this; }
    public BoardMemberBuilder ForUser(Guid userId) { _userId = userId; return this; }
    public BoardMemberBuilder WithRole(BoardRole role) { _role = role; return this; }
    public BoardMemberBuilder AsOwner() { _role = BoardRole.Owner; return this; }
    public BoardMemberBuilder AsViewer() { _role = BoardRole.Viewer; return this; }
    public BoardMemberBuilder InvitedBy(Guid invitedByUserId) { _invitedByUserId = invitedByUserId; return this; }
    public BoardMemberBuilder JoinedAt(DateTimeOffset joinedAt) { _joinedAt = joinedAt; return this; }

    public BoardMember Build() =>
        new(
            id: _id,
            boardId: _boardId,
            userId: _userId,
            role: _role,
            invitedByUserId: _invitedByUserId,
            joinedAt: _joinedAt);
}
