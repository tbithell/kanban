using System.ComponentModel.DataAnnotations;

namespace Kanban.Contracts;

public sealed record MoveLaneRequest(
    [Range(1, int.MaxValue)] int TargetPosition,
    [Range(1, int.MaxValue)] int ExpectedVersion
);
