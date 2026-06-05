using System.Data;
using Kanban.Domain.Entities;

namespace Kanban.DataAccess.Interfaces;

public interface IBoardRepository
{
    Task<Board?> FindBoardForMemberAsync(Guid boardId, Guid userId, IDbTransaction? tx = null);
    Task<IReadOnlyList<Board>> FindBoardsForUserAsync(Guid userId, IDbTransaction? tx = null);
    Task InsertAsync(Board board, IDbTransaction tx);
    Task<bool> ExistsWithNameAsync(string name, IDbTransaction? tx = null);
    Task DeleteAsync(Guid boardId, IDbTransaction tx);
}
