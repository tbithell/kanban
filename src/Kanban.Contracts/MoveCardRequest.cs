using System.ComponentModel.DataAnnotations;

namespace Kanban.Contracts;

public sealed record MoveCardRequest(
    [Required] Guid TargetLaneId,
    [Range(1, int.MaxValue)] int TargetPosition,
    [Range(1, int.MaxValue)] int ExpectedVersion
);
