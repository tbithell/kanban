using System.Data;
using Dapper;
using Kanban.DataAccess.Interfaces;
using Kanban.Domain;
using Kanban.Domain.Entities;

namespace Kanban.DataAccess.Repositories;

public sealed class BoardRepository : IBoardRepository
{
    private readonly IDbConnection _connection;

    public BoardRepository(IDbConnection connection)
    {
        Verify.That(connection).IsNotNull();
        _connection = connection;
    }

    public async Task<Board?> FindBoardForMemberAsync(Guid boardId, Guid userId, IDbTransaction? tx = null)
    {
        Verify.That(boardId).IsNotDefault();
        Verify.That(userId).IsNotDefault();

        const string sql = """
            SELECT b.id, b.name, b.created_by_user_id AS CreatedByUserId, b.created_at AS CreatedAt
            FROM boards b
            INNER JOIN board_members bm ON bm.board_id = b.id
            WHERE b.id = @boardId
              AND bm.user_id = @userId
            """;

        return await _connection.QuerySingleOrDefaultAsync<Board>(
            sql, new { boardId = boardId.ToString("D"), userId = userId.ToString("D") }, transaction: tx);
    }

    public async Task<IReadOnlyList<Board>> FindBoardsForUserAsync(Guid userId, IDbTransaction? tx = null)
    {
        Verify.That(userId).IsNotDefault();

        const string sql = """
            SELECT b.id, b.name, b.created_by_user_id AS CreatedByUserId, b.created_at AS CreatedAt
            FROM boards b
            INNER JOIN board_members bm ON bm.board_id = b.id
            WHERE bm.user_id = @userId
            ORDER BY b.created_at ASC
            """;

        var results = await _connection.QueryAsync<Board>(
            sql, new { userId = userId.ToString("D") }, transaction: tx);
        return results.AsList();
    }

    public async Task InsertAsync(Board board, IDbTransaction tx)
    {
        Verify.That(board).IsNotNull();
        Verify.That(tx).IsNotNull();

        const string sql = """
            INSERT INTO boards (id, name, created_by_user_id, created_at)
            VALUES (@id, @name, @createdByUserId, @createdAt)
            """;

        await _connection.ExecuteAsync(sql, new
        {
            id = board.Id.ToString("D"),
            name = board.Name,
            createdByUserId = board.CreatedByUserId.ToString("D"),
            createdAt = board.CreatedAt.ToString("o"),
        }, transaction: tx);
    }

    public async Task<bool> ExistsWithNameAsync(string name, IDbTransaction? tx = null)
    {
        Verify.That(name).IsNotNull().IsNotEmpty();

        const string sql = "SELECT COUNT(*) FROM boards WHERE name = @name";
        var count = await _connection.QuerySingleAsync<int>(sql, new { name }, transaction: tx);
        return count > 0;
    }

    public async Task DeleteAsync(Guid boardId, IDbTransaction tx)
    {
        Verify.That(boardId).IsNotDefault();
        Verify.That(tx).IsNotNull();

        const string sql = "DELETE FROM boards WHERE id = @boardId";
        await _connection.ExecuteAsync(sql, new { boardId = boardId.ToString("D") }, transaction: tx);
    }
}
