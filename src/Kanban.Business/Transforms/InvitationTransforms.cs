using FluentValidation;
using Kanban.Contracts;
using Kanban.Domain;
using Kanban.Domain.Entities;

namespace Kanban.Business.Transforms;

public static class InvitationTransforms
{
    public static IssueInviteResponse ToResponse(Invitation invitation, string rawToken,
                                                  string frontendBaseUrl)
    {
        ArgumentNullException.ThrowIfNull(invitation);
        new InlineValidator<string> { v => v.RuleFor(x => x).NotEmpty().WithName("rawToken") }.ValidateAndThrow(rawToken);
        new InlineValidator<string> { v => v.RuleFor(x => x).NotEmpty().WithName("frontendBaseUrl") }.ValidateAndThrow(frontendBaseUrl);
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
        new InlineValidator<string> { v => v.RuleFor(x => x).NotEmpty().WithName("rawToken") }.ValidateAndThrow(rawToken);
        new InlineValidator<string> { v => v.RuleFor(x => x).NotEmpty().WithName("frontendBaseUrl") }.ValidateAndThrow(frontendBaseUrl);
        return new IssueInviteResponse
        {
            Token = rawToken,
            RedemptionLink = $"{frontendBaseUrl}/accept/{rawToken}",
            ExpiresAt = expiresAt,
        };
    }
}
