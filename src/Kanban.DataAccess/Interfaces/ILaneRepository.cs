using System.Data;
using Kanban.Domain.Entities;

namespace Kanban.DataAccess.Interfaces;

public interface ILaneRepository
{
    Task<IReadOnlyList<Lane>> FindByBoardAsync(Guid boardId, IDbTransaction? tx = null);
    Task<Lane?> FindByIdAsync(Guid laneId, IDbTransaction? tx = null);
    Task InsertAsync(Lane lane, IDbTransaction tx);
    Task UpdateNameAsync(Guid laneId, string name, IDbTransaction tx);
    Task<int> UpdatePositionAsync(Guid laneId, int newPosition, int expectedVersion, IDbTransaction tx);
    Task ShiftPositionsAsync(Guid boardId, int fromPosition, int toPosition, int delta, IDbTransaction tx);
    Task DeleteAsync(Guid laneId, IDbTransaction tx);
    Task<int> CountInBoardAsync(Guid boardId, IDbTransaction? tx = null);
    Task<bool> ExistsWithNameInBoardAsync(Guid boardId, string name, IDbTransaction? tx = null);
}
