using FluentValidation;
using Kanban.Contracts;

namespace Kanban.Api.Validators;

public sealed class RenameLaneRequestValidator : AbstractValidator<RenameLaneRequest>
{
    public RenameLaneRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
