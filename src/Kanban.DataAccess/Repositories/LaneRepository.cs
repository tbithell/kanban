using System.Data;
using Dapper;
using Kanban.DataAccess.Interfaces;
using Kanban.Domain;
using Kanban.Domain.Entities;

namespace Kanban.DataAccess.Repositories;

public sealed class LaneRepository : ILaneRepository
{
    private readonly IDbConnection _connection;

    public LaneRepository(IDbConnection connection)
    {
        Verify.That(connection).IsNotNull();
        _connection = connection;
    }

    public async Task<IReadOnlyList<Lane>> FindByBoardAsync(Guid boardId, IDbTransaction? tx = null)
    {
        Verify.That(boardId).IsNotDefault();

        const string sql = """
            SELECT id, board_id AS BoardId, name, position, version, created_at AS CreatedAt
            FROM lanes
            WHERE board_id = @boardId
            ORDER BY position ASC
            """;

        var results = await _connection.QueryAsync<Lane>(
            sql, new { boardId = boardId.ToString("D") }, transaction: tx);
        return results.AsList();
    }

    public async Task<Lane?> FindByIdAsync(Guid laneId, IDbTransaction? tx = null)
    {
        Verify.That(laneId).IsNotDefault();

        const string sql = """
            SELECT id, board_id AS BoardId, name, position, version, created_at AS CreatedAt
            FROM lanes
            WHERE id = @laneId
            """;

        return await _connection.QuerySingleOrDefaultAsync<Lane>(
            sql, new { laneId = laneId.ToString("D") }, transaction: tx);
    }

    public async Task InsertAsync(Lane lane, IDbTransaction tx)
    {
        Verify.That(lane).IsNotNull();
        Verify.That(tx).IsNotNull();

        const string sql = """
            INSERT INTO lanes (id, board_id, name, position, version, created_at)
            VALUES (@id, @boardId, @name, @position, @version, @createdAt)
            """;

        await _connection.ExecuteAsync(sql, new
        {
            id = lane.Id.ToString("D"),
            boardId = lane.BoardId.ToString("D"),
            name = lane.Name,
            position = lane.Position,
            version = lane.Version,
            createdAt = lane.CreatedAt.ToString("o"),
        }, transaction: tx);
    }

    public async Task UpdateNameAsync(Guid laneId, string name, IDbTransaction tx)
    {
        Verify.That(laneId).IsNotDefault();
        Verify.That(name).IsNotNull().IsNotEmpty();
        Verify.That(tx).IsNotNull();

        const string sql = "UPDATE lanes SET name = @name WHERE id = @laneId";
        await _connection.ExecuteAsync(sql,
            new { laneId = laneId.ToString("D"), name }, transaction: tx);
    }

    public async Task<int> UpdatePositionAsync(Guid laneId, int newPosition, int expectedVersion, IDbTransaction tx)
    {
        Verify.That(laneId).IsNotDefault();
        Verify.That(tx).IsNotNull();

        const string sql = """
            UPDATE lanes
            SET position = @newPosition, version = version + 1
            WHERE id = @laneId
              AND version = @expectedVersion
            """;

        return await _connection.ExecuteAsync(sql, new
        {
            laneId = laneId.ToString("D"),
            newPosition,
            expectedVersion,
        }, transaction: tx);
    }

    public async Task ShiftPositionsAsync(Guid boardId, int fromPosition, int toPosition, int delta, IDbTransaction tx)
    {
        Verify.That(boardId).IsNotDefault();
        Verify.That(tx).IsNotNull();

        const string sql = """
            UPDATE lanes
            SET position = position + @delta
            WHERE board_id = @boardId
              AND position >= @fromPosition
              AND position <= @toPosition
            """;

        await _connection.ExecuteAsync(sql, new
        {
            boardId = boardId.ToString("D"),
            fromPosition,
            toPosition,
            delta,
        }, transaction: tx);
    }

    public async Task DeleteAsync(Guid laneId, IDbTransaction tx)
    {
        Verify.That(laneId).IsNotDefault();
        Verify.That(tx).IsNotNull();

        const string sql = "DELETE FROM lanes WHERE id = @laneId";
        await _connection.ExecuteAsync(sql,
            new { laneId = laneId.ToString("D") }, transaction: tx);
    }

    public async Task<int> CountInBoardAsync(Guid boardId, IDbTransaction? tx = null)
    {
        Verify.That(boardId).IsNotDefault();

        const string sql = "SELECT COUNT(*) FROM lanes WHERE board_id = @boardId";
        return await _connection.QuerySingleAsync<int>(
            sql, new { boardId = boardId.ToString("D") }, transaction: tx);
    }
}
