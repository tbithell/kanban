using Kanban.Contracts;
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
}
