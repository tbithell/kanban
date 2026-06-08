using System.ComponentModel.DataAnnotations;

namespace Kanban.Contracts;

public sealed record CreateCardRequest(
    [Required, MaxLength(200)] string Title,
    [MaxLength(2000)] string? Description = null,
    DateOnly? DueDate = null
);
