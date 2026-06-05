using System.ComponentModel.DataAnnotations;

namespace Kanban.Contracts;

public sealed record ChangeMemberRoleRequest(
    [Required] BoardRoleDto Role
);
