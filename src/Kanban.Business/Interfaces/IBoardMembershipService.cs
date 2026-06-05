using Kanban.Contracts;

namespace Kanban.Business.Interfaces;

public interface IBoardMembershipService
{
    Task<IReadOnlyList<BoardMemberDto>> ListMembersAsync(Guid boardId);
    Task InviteAsync(Guid boardId, InviteBoardMemberRequest request);
    Task ChangeRoleAsync(Guid boardId, Guid userId, ChangeMemberRoleRequest request);
    Task RemoveMemberAsync(Guid boardId, Guid userId);
}
