using System.Data;
using Kanban.Domain.Entities;

namespace Kanban.DataAccess.Interfaces;

public interface ICardRepository
{
    Task<IReadOnlyList<Card>> FindByLaneAsync(Guid laneId, IDbTransaction? tx = null);
    Task<Card?> FindByIdAsync(Guid cardId, IDbTransaction? tx = null);
    Task InsertAsync(Card card, IDbTransaction tx);
    Task UpdateAsync(Card card, IDbTransaction tx);
    Task<int> UpdatePositionAsync(Guid cardId, Guid laneId, int newPosition, int expectedVersion, IDbTransaction tx);
    Task ShiftPositionsInLaneAsync(Guid laneId, int fromPosition, int toPosition, int delta, IDbTransaction tx);
    Task DeleteAsync(Guid cardId, IDbTransaction tx);
    Task<int> CountInLaneAsync(Guid laneId, IDbTransaction? tx = null);
}
