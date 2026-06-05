using FluentValidation;
using Kanban.Contracts;

namespace Kanban.Api.Validators;

public sealed class MoveCardRequestValidator : AbstractValidator<MoveCardRequest>
{
    public MoveCardRequestValidator()
    {
        RuleFor(x => x.TargetLaneId).NotEqual(Guid.Empty);
        RuleFor(x => x.TargetPosition).GreaterThanOrEqualTo(1);
        RuleFor(x => x.ExpectedVersion).GreaterThanOrEqualTo(1);
    }
}
