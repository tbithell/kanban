using Kanban.Domain.Entities;

namespace Kanban.Tests.Unit.Builders;

public sealed class LaneBuilder
{
    private Guid _id = Guid.NewGuid();
    private Guid _boardId = Guid.NewGuid();
    private string _name = "Test Lane";
    private int _position = 1;
    private int _version = 1;
    private DateTimeOffset _createdAt = DateTimeOffset.UtcNow;

    public static LaneBuilder ALane() => new();

    public LaneBuilder WithId(Guid id) { _id = id; return this; }
    public LaneBuilder OnBoard(Guid boardId) { _boardId = boardId; return this; }
    public LaneBuilder WithName(string name) { _name = name; return this; }
    public LaneBuilder AtPosition(int position) { _position = position; return this; }
    public LaneBuilder WithVersion(int version) { _version = version; return this; }
    public LaneBuilder CreatedAt(DateTimeOffset createdAt) { _createdAt = createdAt; return this; }

    public Lane Build() =>
        new(
            id: _id,
            boardId: _boardId,
            name: _name,
            position: _position,
            version: _version,
            createdAt: _createdAt);
}
