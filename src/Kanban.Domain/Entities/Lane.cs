namespace Kanban.Domain.Entities;

public sealed class Lane
{
    public Guid Id { get; }
    public Guid BoardId { get; }
    public string Name { get; private set; }
    public int Position { get; private set; }
    public int Version { get; private set; }
    public DateTimeOffset CreatedAt { get; }

    public Lane(Guid id, Guid boardId, string name, int position, int version, DateTimeOffset createdAt)
    {
        Verify.That(id).IsNotDefault();
        Verify.That(boardId).IsNotDefault();
        Verify.That(name).IsNotNull().IsNotEmpty().HasMaxLength(100);
        Verify.That(position).IsPositive<int>();
        Id = id;
        BoardId = boardId;
        Name = name;
        Position = position;
        Version = version;
        CreatedAt = createdAt;
    }

    public void Rename(string name)
    {
        Verify.That(name).IsNotNull().IsNotEmpty().HasMaxLength(100);
        Name = name;
    }

    public void MoveTo(int position)
    {
        Verify.That(position).IsPositive<int>();
        Position = position;
        Version++;
    }
}
