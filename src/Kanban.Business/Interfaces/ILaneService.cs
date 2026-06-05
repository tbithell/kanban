using Kanban.Contracts;

namespace Kanban.Business.Interfaces;

public interface ILaneService
{
    Task<LaneDto> CreateAsync(Guid boardId, CreateLaneRequest request);
    Task<LaneDto> RenameAsync(Guid boardId, Guid laneId, RenameLaneRequest request);
    Task MoveAsync(Guid boardId, Guid laneId, MoveLaneRequest request);
    Task DeleteAsync(Guid boardId, Guid laneId);
}
