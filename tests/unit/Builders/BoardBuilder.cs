using Kanban.Domain.Entities;

namespace Kanban.Tests.Unit.Builders;

public sealed class BoardBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _name = "Test Board";
    private Guid _createdByUserId = Guid.NewGuid();
    private DateTimeOffset _createdAt = DateTimeOffset.UtcNow;

    public static BoardBuilder ABoard() => new();

    public BoardBuilder WithId(Guid id) { _id = id; return this; }
    public BoardBuilder WithName(string name) { _name = name; return this; }
    public BoardBuilder CreatedBy(Guid userId) { _createdByUserId = userId; return this; }
    public BoardBuilder CreatedAt(DateTimeOffset createdAt) { _createdAt = createdAt; return this; }

    public Board Build() =>
        new(
            id: _id,
            name: _name,
            createdByUserId: _createdByUserId,
            createdAt: _createdAt);
}
