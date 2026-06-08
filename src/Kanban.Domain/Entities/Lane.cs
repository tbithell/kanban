using FluentValidation;

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
        new InlineValidator<Guid> { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("id") }.ValidateAndThrow(id);
        new InlineValidator<Guid> { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("boardId") }.ValidateAndThrow(boardId);
        new InlineValidator<string> { v => v.RuleFor(x => x).NotEmpty().MaximumLength(100).WithName("name") }.ValidateAndThrow(name);
        new InlineValidator<int> { v => v.RuleFor(x => x).GreaterThan(0).WithName("position") }.ValidateAndThrow(position);
        Id = id;
        BoardId = boardId;
        Name = name;
        Position = position;
        Version = version;
        CreatedAt = createdAt;
    }

    public void Rename(string name)
    {
        new InlineValidator<string> { v => v.RuleFor(x => x).NotEmpty().MaximumLength(100).WithName("name") }.ValidateAndThrow(name);
        Name = name;
    }

    public void MoveTo(int position)
    {
        new InlineValidator<int> { v => v.RuleFor(x => x).GreaterThan(0).WithName("position") }.ValidateAndThrow(position);
        Position = position;
        Version++;
    }
}
