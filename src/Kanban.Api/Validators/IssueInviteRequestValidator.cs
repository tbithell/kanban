using FluentValidation;
using Kanban.Contracts;

namespace Kanban.Api.Validators;

public sealed class IssueInviteRequestValidator : AbstractValidator<IssueInviteRequest>
{
    public IssueInviteRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .WithMessage("Must be a valid email address.");
    }
}
