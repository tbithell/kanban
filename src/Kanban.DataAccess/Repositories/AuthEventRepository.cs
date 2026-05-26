using System.Data;
using Dapper;
using Kanban.DataAccess.Interfaces;
using Kanban.Domain;
using Kanban.Domain.Events;

namespace Kanban.DataAccess.Repositories;

public sealed class AuthEventRepository : IAuthEventRepository
{
    private readonly IDbConnection _connection;

    public AuthEventRepository(IDbConnection connection)
    {
        Verify.That(connection).IsNotNull();
        _connection = connection;
    }

    public async Task RecordAsync(AuthEvent authEvent, IDbTransaction tx)
    {
        Verify.That(authEvent).IsNotNull();

        const string sql = """
            INSERT INTO auth_events (id, occurred_at, event_type, user_id, outcome)
            VALUES (@id, @occurredAt, @eventType, @userId, @outcome)
            """;

        await _connection.ExecuteAsync(sql, new
        {
            id = authEvent.Id.ToString("D"),
            occurredAt = authEvent.OccurredAt.ToString("o"),
            eventType = authEvent.EventType.ToString(),
            userId = authEvent.UserId?.ToString("D"),
            outcome = authEvent.Outcome,
        }, transaction: tx);
    }
}
