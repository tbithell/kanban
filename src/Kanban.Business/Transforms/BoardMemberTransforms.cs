using Kanban.Contracts;
using Kanban.Domain.Entities;
using Kanban.Domain.Enums;

namespace Kanban.Business.Transforms;

public static class BoardMemberTransforms
{
    public static BoardMemberDto ToDto(BoardMember member, string displayName) =>
        new(
            UserId: member.UserId,
            DisplayName: displayName,
            Role: member.Role switch
            {
                BoardRole.Owner  => BoardRoleDto.Owner,
                BoardRole.Member => BoardRoleDto.Member,
                BoardRole.Viewer => BoardRoleDto.Viewer,
                _                => throw new ArgumentOutOfRangeException(nameof(member.Role)),
            },
            JoinedAt: member.JoinedAt);
}
