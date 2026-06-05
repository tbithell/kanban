using System.ComponentModel.DataAnnotations;

namespace Kanban.Contracts;

public sealed record CreateLaneRequest(
    [Required, MaxLength(100)] string Name
);
