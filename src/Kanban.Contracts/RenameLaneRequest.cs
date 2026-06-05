using System.ComponentModel.DataAnnotations;

namespace Kanban.Contracts;

public sealed record RenameLaneRequest(
    [Required, MaxLength(100)] string Name
);
