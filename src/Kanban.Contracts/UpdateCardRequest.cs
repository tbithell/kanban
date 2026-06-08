using System.ComponentModel.DataAnnotations;

namespace Kanban.Contracts;

public sealed record UpdateCardRequest(
    [MaxLength(200)] string? Title,
    string? Description,
    bool ClearDueDate,
    DateOnly? DueDate
);
