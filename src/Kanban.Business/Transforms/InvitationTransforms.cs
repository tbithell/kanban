using Kanban.Contracts;
using Kanban.Domain;
using Kanban.Domain.Entities;

namespace Kanban.Business.Transforms;

public static class InvitationTransforms
{
    public static IssueInviteResponse ToResponse(Invitation invitation, string rawToken,
                                                  string frontendBaseUrl)
    {
        Verify.That(invitation).IsNotNull();
        Verify.That(rawToken).IsNotNull().IsNotEmpty();
        Verify.That(frontendBaseUrl).IsNotNull().IsNotEmpty();
        return new IssueInviteResponse
        {
            Token = rawToken,
            RedemptionLink = $"{frontendBaseUrl}/accept/{rawToken}",
            ExpiresAt = invitation.ExpiresAt,
        };
    }

    public static IssueInviteResponse ToResponse(string rawToken, DateTimeOffset expiresAt,
                                                   string frontendBaseUrl)
    {
        Verify.That(rawToken).IsNotNull().IsNotEmpty();
        Verify.That(frontendBaseUrl).IsNotNull().IsNotEmpty();
        return new IssueInviteResponse
        {
            Token = rawToken,
            RedemptionLink = $"{frontendBaseUrl}/accept/{rawToken}",
            ExpiresAt = expiresAt,
        };
    }
}
