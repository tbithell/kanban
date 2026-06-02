using Kanban.Contracts;
using Kanban.Domain.Entities;
using Kanban.Domain.Enums;

namespace Kanban.Business.Interfaces;

public interface IInvitationService
{
    Task<(IssueInviteResponse Response, bool IsNew)> IssueAsync(
        string email,
        Guid issuedByUserId,
        SystemRole callerRole,
        string frontendBaseUrl,
        CancellationToken cancellationToken = default);

    Task<User> AcceptAsync(
        string rawToken,
        string googleEmail,
        string googleSub,
        string displayName,
        CancellationToken cancellationToken = default);
}
