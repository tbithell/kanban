using System.Security.Claims;

namespace Kanban.Business.Interfaces;

public interface IAuthService
{
    Task HandleSignInAsync(
        string googleSub,
        string email,
        ClaimsIdentity claimsIdentity,
        CancellationToken cancellationToken = default);
}
