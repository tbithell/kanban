using Kanban.Domain.Entities;

namespace Kanban.Tests.Unit.Builders;

public sealed class CardBuilder
{
    private Guid _id = Guid.NewGuid();
    private Guid _laneId = Guid.NewGuid();
    private Guid _boardId = Guid.NewGuid();
    private string _title = "Test Card";
    private string? _description;
    private DateOnly? _dueDate;
    private int _position = 1;
    private int _version = 1;
    private DateTimeOffset _createdAt = DateTimeOffset.UtcNow;
    private DateTimeOffset _updatedAt = DateTimeOffset.UtcNow;

    public static CardBuilder ACard() => new();

    public CardBuilder WithId(Guid id) { _id = id; return this; }
    public CardBuilder InLane(Guid laneId) { _laneId = laneId; return this; }
    public CardBuilder OnBoard(Guid boardId) { _boardId = boardId; return this; }
    public CardBuilder WithTitle(string title) { _title = title; return this; }
    public CardBuilder WithDescription(string description) { _description = description; return this; }
    public CardBuilder DueOn(DateOnly dueDate) { _dueDate = dueDate; return this; }
    public CardBuilder AtPosition(int position) { _position = position; return this; }
    public CardBuilder WithVersion(int version) { _version = version; return this; }
    public CardBuilder CreatedAt(DateTimeOffset createdAt) { _createdAt = createdAt; return this; }
    public CardBuilder UpdatedAt(DateTimeOffset updatedAt) { _updatedAt = updatedAt; return this; }

    public Card Build() =>
        new(
            id: _id,
            laneId: _laneId,
            boardId: _boardId,
            title: _title,
            description: _description,
            dueDate: _dueDate,
            position: _position,
            version: _version,
            createdAt: _createdAt,
            updatedAt: _updatedAt);
}
