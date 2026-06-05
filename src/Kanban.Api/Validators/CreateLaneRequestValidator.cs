using FluentValidation;
using Kanban.Contracts;

namespace Kanban.Api.Validators;

public sealed class CreateLaneRequestValidator : AbstractValidator<CreateLaneRequest>
{
    public CreateLaneRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
