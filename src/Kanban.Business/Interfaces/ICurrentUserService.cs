using System.Security.Claims;

namespace Kanban.Business.Interfaces;

public interface ICurrentUserService
{
    bool IsAuthenticated { get; }
    bool IsRegistered { get; }
    Guid? UserId { get; }
    string? SystemRole { get; }
    ClaimsPrincipal Principal { get; }
}
