using System.Security.Claims;
using Kanban.Contracts;

namespace Kanban.Business.Interfaces;

public interface IAuthService
{
    Task HandleSignInAsync(
        string googleSub,
        string email,
        ClaimsIdentity claimsIdentity,
        CancellationToken cancellationToken = default);

    Task<CurrentUserDto?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
