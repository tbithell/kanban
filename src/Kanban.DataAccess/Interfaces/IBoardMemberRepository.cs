using System.Data;
using Kanban.Domain.Entities;
using Kanban.Domain.Enums;

namespace Kanban.DataAccess.Interfaces;

public interface IBoardMemberRepository
{
    Task<BoardRole?> FindRoleAsync(Guid boardId, Guid userId, IDbTransaction? tx = null);
    Task<int> CountOwnersAsync(Guid boardId, IDbTransaction? tx = null);
    Task InsertAsync(BoardMember member, IDbTransaction tx);
    Task DeleteAsync(Guid boardId, Guid userId, IDbTransaction tx);
    Task UpdateRoleAsync(Guid boardId, Guid userId, BoardRole newRole, IDbTransaction tx);
    Task<IReadOnlyList<BoardMember>> FindAllForBoardAsync(Guid boardId, IDbTransaction? tx = null);
}
