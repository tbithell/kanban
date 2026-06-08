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
        Guid? boardId = null,
        BoardRole? boardRole = null,
        CancellationToken cancellationToken = default);

    Task<(User User, Guid? BoardId)> AcceptAsync(
        string rawToken,
        string googleEmail,
        string googleSub,
        string displayName,
        CancellationToken cancellationToken = default);
}
