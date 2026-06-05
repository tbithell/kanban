namespace Kanban.Domain.Entities;

public sealed class Board
{
    public Guid Id { get; }
    public string Name { get; private set; }
    public Guid CreatedByUserId { get; }
    public DateTimeOffset CreatedAt { get; }

    public Board(Guid id, string name, Guid createdByUserId, DateTimeOffset createdAt)
    {
        Verify.That(id).IsNotDefault();
        Verify.That(name).IsNotNull().IsNotEmpty().HasMaxLength(200);
        Verify.That(createdByUserId).IsNotDefault();
        Id = id;
        Name = name;
        CreatedByUserId = createdByUserId;
        CreatedAt = createdAt;
    }

    public void Rename(string name)
    {
        Verify.That(name).IsNotNull().IsNotEmpty().HasMaxLength(200);
        Name = name;
    }
}
