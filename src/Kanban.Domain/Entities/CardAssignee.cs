namespace Kanban.Domain.Entities;

public sealed class CardAssignee
{
    public Guid Id { get; }
    public Guid CardId { get; }
    public Guid UserId { get; }
    public DateTimeOffset AssignedAt { get; }

    public CardAssignee(Guid id, Guid cardId, Guid userId, DateTimeOffset assignedAt)
    {
        Verify.That(id).IsNotDefault();
        Verify.That(cardId).IsNotDefault();
        Verify.That(userId).IsNotDefault();
        Id = id;
        CardId = cardId;
        UserId = userId;
        AssignedAt = assignedAt;
    }
}
