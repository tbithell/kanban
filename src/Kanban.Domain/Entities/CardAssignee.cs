using FluentValidation;

namespace Kanban.Domain.Entities;

public sealed class CardAssignee
{
    public Guid Id { get; }
    public Guid CardId { get; }
    public Guid UserId { get; }
    public DateTimeOffset AssignedAt { get; }

    public CardAssignee(Guid id, Guid cardId, Guid userId, DateTimeOffset assignedAt)
    {
        new InlineValidator<Guid> { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("id") }.ValidateAndThrow(id);
        new InlineValidator<Guid> { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("cardId") }.ValidateAndThrow(cardId);
        new InlineValidator<Guid> { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("userId") }.ValidateAndThrow(userId);
        Id = id;
        CardId = cardId;
        UserId = userId;
        AssignedAt = assignedAt;
    }
}
