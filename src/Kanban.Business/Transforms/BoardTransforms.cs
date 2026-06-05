using Kanban.Contracts;
using Kanban.Domain.Entities;
using Kanban.Domain.Enums;

namespace Kanban.Business.Transforms;

public static class BoardTransforms
{
    public static BoardSummaryDto ToSummaryDto(Board board, BoardRole callerRole, int laneCount, int cardCount) =>
        new(
            Id: board.Id,
            Name: board.Name,
            LaneCount: laneCount,
            CardCount: cardCount,
            CallerRole: ToRoleDto(callerRole));

    public static BoardDetailDto ToDetailDto(Board board, BoardRole callerRole, IReadOnlyList<LaneDto> lanes) =>
        new(
            Id: board.Id,
            Name: board.Name,
            CreatedByUserId: board.CreatedByUserId,
            CreatedAt: board.CreatedAt,
            CallerRole: ToRoleDto(callerRole),
            Lanes: lanes);

    private static BoardRoleDto ToRoleDto(BoardRole role) => role switch
    {
        BoardRole.Owner  => BoardRoleDto.Owner,
        BoardRole.Member => BoardRoleDto.Member,
        BoardRole.Viewer => BoardRoleDto.Viewer,
        _                => throw new ArgumentOutOfRangeException(nameof(role)),
    };
}
