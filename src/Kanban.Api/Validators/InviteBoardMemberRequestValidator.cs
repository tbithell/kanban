using FluentValidation;
using Kanban.Contracts;

namespace Kanban.Api.Validators;

public sealed class InviteBoardMemberRequestValidator : AbstractValidator<InviteBoardMemberRequest>
{
    public InviteBoardMemberRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.Role).IsInEnum();
    }
}
