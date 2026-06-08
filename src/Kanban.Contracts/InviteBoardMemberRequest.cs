using System.ComponentModel.DataAnnotations;

namespace Kanban.Contracts;

public sealed record InviteBoardMemberRequest(
    [Required, EmailAddress, MaxLength(254)] string Email,
    [Required] BoardRoleDto Role
);
