using Kanban.Domain.Enums;

namespace Kanban.Domain.Events;

public sealed record AuthEvent(
    Guid Id,
    DateTimeOffset OccurredAt,
    AuthEventType EventType,
    Guid? UserId,
    string Outcome);
