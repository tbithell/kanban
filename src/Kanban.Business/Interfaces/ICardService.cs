using Kanban.Contracts;

namespace Kanban.Business.Interfaces;

public interface ICardService
{
    Task<CardDto> CreateAsync(Guid boardId, Guid laneId, CreateCardRequest request);
    Task<CardDto> UpdateAsync(Guid boardId, Guid cardId, UpdateCardRequest request);
    Task<CardDto> MoveAsync(Guid boardId, Guid cardId, MoveCardRequest request);
    Task DeleteAsync(Guid boardId, Guid cardId);
}
