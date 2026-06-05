using Kanban.Contracts;

namespace Kanban.Business.Interfaces;

public interface IBoardService
{
    Task<BoardDetailDto> CreateAsync(CreateBoardRequest request);
    Task<IReadOnlyList<BoardSummaryDto>> ListForCallerAsync();
    Task<BoardDetailDto> GetAsync(Guid boardId);
    Task DeleteAsync(Guid boardId);
}
