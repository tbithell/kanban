using Kanban.Contracts;
using Kanban.Domain;
using Kanban.Domain.Entities;
using Kanban.Domain.Enums;

namespace Kanban.Business.Transforms;

public static class UserTransforms
{
    public static CurrentUserDto ToDto(User user)
    {
        Verify.That(user).IsNotNull();
        return new CurrentUserDto
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            SystemRole = user.SystemRole == SystemRole.Admin ? "admin" : "standard",
            RegisteredAt = user.RegisteredAt,
            LastSignInAt = user.LastSignInAt,
        };
    }
}
