using System.Data;
using Dapper;
using FluentValidation;
using Kanban.DataAccess.Interfaces;
using Kanban.Domain;
using Kanban.Domain.Entities;

namespace Kanban.DataAccess.Repositories;

public sealed class CardRepository : ICardRepository
{
    private readonly IDbConnection _connection;

    public CardRepository(IDbConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
    }

    public async Task<IReadOnlyList<Card>> FindByLaneAsync(Guid laneId, IDbTransaction? tx = null)
    {
        new InlineValidator<Guid> { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("laneId") }.ValidateAndThrow(laneId);

        const string sql = """
            SELECT id, lane_id AS LaneId, board_id AS BoardId, title, description, due_date AS DueDate,
                   position, version, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM cards
            WHERE lane_id = @laneId
            ORDER BY position ASC
            """;

        var results = await _connection.QueryAsync<Card>(
            sql, new { laneId = laneId.ToString("D") }, transaction: tx);
        return results.AsList();
    }

    public async Task<Card?> FindByIdAsync(Guid cardId, IDbTransaction? tx = null)
    {
        new InlineValidator<Guid> { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("cardId") }.ValidateAndThrow(cardId);

        const string sql = """
            SELECT id, lane_id AS LaneId, board_id AS BoardId, title, description, due_date AS DueDate,
                   position, version, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM cards
            WHERE id = @cardId
            """;

        return await _connection.QuerySingleOrDefaultAsync<Card>(
            sql, new { cardId = cardId.ToString("D") }, transaction: tx);
    }

    public async Task InsertAsync(Card card, IDbTransaction tx)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(tx);

        const string sql = """
            INSERT INTO cards (id, lane_id, board_id, title, description, due_date,
                               position, version, created_at, updated_at)
            VALUES (@id, @laneId, @boardId, @title, @description, @dueDate,
                    @position, @version, @createdAt, @updatedAt)
            """;

        await _connection.ExecuteAsync(sql, new
        {
            id = card.Id.ToString("D"),
            laneId = card.LaneId.ToString("D"),
            boardId = card.BoardId.ToString("D"),
            title = card.Title,
            description = card.Description,
            dueDate = card.DueDate?.ToString("o"),
            position = card.Position,
            version = card.Version,
            createdAt = card.CreatedAt.ToString("o"),
            updatedAt = card.UpdatedAt.ToString("o"),
        }, transaction: tx);
    }

    public async Task UpdateAsync(Card card, IDbTransaction tx)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(tx);

        const string sql = """
            UPDATE cards
            SET title = @title, description = @description, due_date = @dueDate, updated_at = @updatedAt
            WHERE id = @id
            """;

        await _connection.ExecuteAsync(sql, new
        {
            id = card.Id.ToString("D"),
            title = card.Title,
            description = card.Description,
            dueDate = card.DueDate?.ToString("o"),
            updatedAt = card.UpdatedAt.ToString("o"),
        }, transaction: tx);
    }

    public async Task<int> UpdatePositionAsync(Guid cardId, Guid laneId, int newPosition, int expectedVersion, IDbTransaction tx)
    {
        new InlineValidator<Guid> { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("cardId") }.ValidateAndThrow(cardId);
        new InlineValidator<Guid> { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("laneId") }.ValidateAndThrow(laneId);
        ArgumentNullException.ThrowIfNull(tx);

        const string sql = """
            UPDATE cards
            SET lane_id = @laneId, position = @newPosition, version = version + 1,
                updated_at = @updatedAt
            WHERE id = @cardId
              AND version = @expectedVersion
            """;

        return await _connection.ExecuteAsync(sql, new
        {
            cardId = cardId.ToString("D"),
            laneId = laneId.ToString("D"),
            newPosition,
            expectedVersion,
            updatedAt = DateTimeOffset.UtcNow.ToString("o"),
        }, transaction: tx);
    }

    public async Task ShiftPositionsInLaneAsync(Guid laneId, int fromPosition, int toPosition, int delta, IDbTransaction tx)
    {
        new InlineValidator<Guid> { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("laneId") }.ValidateAndThrow(laneId);
        ArgumentNullException.ThrowIfNull(tx);

        const string sql = """
            UPDATE cards
            SET position = position + @delta
            WHERE lane_id = @laneId
              AND position >= @fromPosition
              AND position <= @toPosition
            """;

        await _connection.ExecuteAsync(sql, new
        {
            laneId = laneId.ToString("D"),
            fromPosition,
            toPosition,
            delta,
        }, transaction: tx);
    }

    public async Task DeleteAsync(Guid cardId, IDbTransaction tx)
    {
        new InlineValidator<Guid> { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("cardId") }.ValidateAndThrow(cardId);
        ArgumentNullException.ThrowIfNull(tx);

        const string sql = "DELETE FROM cards WHERE id = @cardId";
        await _connection.ExecuteAsync(sql,
            new { cardId = cardId.ToString("D") }, transaction: tx);
    }

    public async Task<int> CountInLaneAsync(Guid laneId, IDbTransaction? tx = null)
    {
        new InlineValidator<Guid> { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("laneId") }.ValidateAndThrow(laneId);

        const string sql = "SELECT COUNT(*) FROM cards WHERE lane_id = @laneId";
        return await _connection.QuerySingleAsync<int>(
            sql, new { laneId = laneId.ToString("D") }, transaction: tx);
    }
}
