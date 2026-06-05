using System.ComponentModel.DataAnnotations;

namespace Kanban.Contracts;

public sealed record CreateBoardRequest(
    [Required, MaxLength(200)] string Name
);
