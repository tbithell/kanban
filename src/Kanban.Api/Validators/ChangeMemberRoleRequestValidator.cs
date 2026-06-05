using FluentValidation;
using Kanban.Contracts;

namespace Kanban.Api.Validators;

public sealed class ChangeMemberRoleRequestValidator : AbstractValidator<ChangeMemberRoleRequest>
{
    public ChangeMemberRoleRequestValidator()
    {
        RuleFor(x => x.Role).IsInEnum();
    }
}
