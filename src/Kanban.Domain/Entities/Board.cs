using FluentValidation;

namespace Kanban.Domain.Entities;

public sealed class Board
{
    public Guid Id { get; }
    public string Name { get; private set; }
    public Guid CreatedByUserId { get; }
    public DateTimeOffset CreatedAt { get; }

    public Board(Guid id, string name, Guid createdByUserId, DateTimeOffset createdAt)
    {
        new InlineValidator<Guid> { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("id") }.ValidateAndThrow(id);
        new InlineValidator<string> { v => v.RuleFor(x => x).NotEmpty().MaximumLength(200).WithName("name") }.ValidateAndThrow(name);
        new InlineValidator<Guid> { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("createdByUserId") }.ValidateAndThrow(createdByUserId);
        Id = id;
        Name = name;
        CreatedByUserId = createdByUserId;
        CreatedAt = createdAt;
    }

    public void Rename(string name)
    {
        new InlineValidator<string> { v => v.RuleFor(x => x).NotEmpty().MaximumLength(200).WithName("name") }.ValidateAndThrow(name);
        Name = name;
    }
}
