using FluentValidation;
using Kanban.Contracts;

namespace Kanban.Api.Validators;

public sealed class MoveLaneRequestValidator : AbstractValidator<MoveLaneRequest>
{
    public MoveLaneRequestValidator()
    {
        RuleFor(x => x.TargetPosition).GreaterThanOrEqualTo(1);
        RuleFor(x => x.ExpectedVersion).GreaterThanOrEqualTo(1);
    }
}
