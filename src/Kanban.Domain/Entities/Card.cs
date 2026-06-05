namespace Kanban.Domain.Entities;

public sealed class Card
{
    public Guid Id { get; }
    public Guid LaneId { get; private set; }
    public Guid BoardId { get; }
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public int Position { get; private set; }
    public int Version { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public Card(Guid id, Guid laneId, Guid boardId, string title, string? description,
                DateOnly? dueDate, int position, int version,
                DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        Verify.That(id).IsNotDefault();
        Verify.That(laneId).IsNotDefault();
        Verify.That(boardId).IsNotDefault();
        Verify.That(title).IsNotNull().IsNotEmpty().HasMaxLength(200);
        if (description is not null) Verify.That(description).HasMaxLength(2000);
        Verify.That(position).IsPositive<int>();
        Id = id;
        LaneId = laneId;
        BoardId = boardId;
        Title = title;
        Description = description;
        DueDate = dueDate;
        Position = position;
        Version = version;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public void Update(string title, string? description, DateOnly? dueDate, DateTimeOffset updatedAt)
    {
        Verify.That(title).IsNotNull().IsNotEmpty().HasMaxLength(200);
        if (description is not null) Verify.That(description).HasMaxLength(2000);
        Title = title;
        Description = description;
        DueDate = dueDate;
        UpdatedAt = updatedAt;
    }

    public void MoveTo(Guid laneId, int position, DateTimeOffset updatedAt)
    {
        Verify.That(laneId).IsNotDefault();
        Verify.That(position).IsPositive<int>();
        LaneId = laneId;
        Position = position;
        Version++;
        UpdatedAt = updatedAt;
    }
}
