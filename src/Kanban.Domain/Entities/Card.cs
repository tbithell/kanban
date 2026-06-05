using FluentValidation;

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
        new InlineValidator<Guid> { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("id") }.ValidateAndThrow(id);
        new InlineValidator<Guid> { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("laneId") }.ValidateAndThrow(laneId);
        new InlineValidator<Guid> { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("boardId") }.ValidateAndThrow(boardId);
        new InlineValidator<string> { v => v.RuleFor(x => x).NotEmpty().MaximumLength(200).WithName("title") }.ValidateAndThrow(title);
        if (description is not null)
            new InlineValidator<string> { v => v.RuleFor(x => x).MaximumLength(2000).WithName("description") }.ValidateAndThrow(description);
        new InlineValidator<int> { v => v.RuleFor(x => x).GreaterThan(0).WithName("position") }.ValidateAndThrow(position);
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
        new InlineValidator<string> { v => v.RuleFor(x => x).NotEmpty().MaximumLength(200).WithName("title") }.ValidateAndThrow(title);
        if (description is not null)
            new InlineValidator<string> { v => v.RuleFor(x => x).MaximumLength(2000).WithName("description") }.ValidateAndThrow(description);
        Title = title;
        Description = description;
        DueDate = dueDate;
        UpdatedAt = updatedAt;
    }

    public void MoveTo(Guid laneId, int position, DateTimeOffset updatedAt)
    {
        new InlineValidator<Guid> { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("laneId") }.ValidateAndThrow(laneId);
        new InlineValidator<int> { v => v.RuleFor(x => x).GreaterThan(0).WithName("position") }.ValidateAndThrow(position);
        LaneId = laneId;
        Position = position;
        Version++;
        UpdatedAt = updatedAt;
    }
}
