using System.Data;
using Dapper;
using Kanban.DataAccess.Interfaces;
using Kanban.Domain;
using Kanban.Domain.Entities;
using Kanban.Domain.Exceptions;

namespace Kanban.DataAccess.Repositories;

public sealed class InvitationRepository : IInvitationRepository
{
    private readonly IDbConnection _connection;

    public InvitationRepository(IDbConnection connection)
    {
        Verify.That(connection).IsNotNull();
        _connection = connection;
    }

    public async Task<Invitation?> FindByTokenHashAsync(string tokenHash, IDbTransaction? tx = null)
    {
        Verify.That(tokenHash).IsNotNull().IsNotEmpty();

        const string sql = """
            SELECT id, email, issued_by_user_id AS IssuedByUserId, token_hash AS TokenHash,
                   issued_at AS IssuedAt, expires_at AS ExpiresAt,
                   consumed_at AS ConsumedAt, consumed_by_user_id AS ConsumedByUserId
            FROM invitations
            WHERE token_hash = @tokenHash
            """;

        return await _connection.QuerySingleOrDefaultAsync<Invitation>(
            sql, new { tokenHash }, transaction: tx);
    }

    public async Task<Invitation?> FindActiveByEmailAsync(string email, IDbTransaction? tx = null)
    {
        Verify.That(email).IsNotNull().IsNotEmpty();

        const string sql = """
            SELECT id, email, issued_by_user_id AS IssuedByUserId, token_hash AS TokenHash,
                   issued_at AS IssuedAt, expires_at AS ExpiresAt,
                   consumed_at AS ConsumedAt, consumed_by_user_id AS ConsumedByUserId
            FROM invitations
            WHERE email = @email
              AND consumed_at IS NULL
              AND expires_at > @now
            """;

        return await _connection.QuerySingleOrDefaultAsync<Invitation>(
            sql, new { email, now = DateTimeOffset.UtcNow.ToString("o") }, transaction: tx);
    }

    public async Task InsertAsync(Invitation invitation, IDbTransaction tx)
    {
        Verify.That(invitation).IsNotNull();
        Verify.That(tx).IsNotNull();

        const string sql = """
            INSERT INTO invitations (id, email, issued_by_user_id, token_hash, issued_at, expires_at,
                                     consumed_at, consumed_by_user_id)
            VALUES (@id, @email, @issuedByUserId, @tokenHash, @issuedAt, @expiresAt,
                    @consumedAt, @consumedByUserId)
            """;

        await _connection.ExecuteAsync(sql, new
        {
            id = invitation.Id.ToString("D"),
            email = invitation.Email,
            issuedByUserId = invitation.IssuedByUserId.ToString("D"),
            tokenHash = invitation.TokenHash,
            issuedAt = invitation.IssuedAt.ToString("o"),
            expiresAt = invitation.ExpiresAt.ToString("o"),
            consumedAt = invitation.ConsumedAt?.ToString("o"),
            consumedByUserId = invitation.ConsumedByUserId?.ToString("D"),
        }, transaction: tx);
    }

    public async Task<bool> TryConsumeAsync(
        string tokenHash, DateTimeOffset consumedAt, IDbTransaction tx)
    {
        Verify.That(tokenHash).IsNotNull().IsNotEmpty();
        Verify.That(tx).IsNotNull();

        const string sql = """
            UPDATE invitations
            SET consumed_at = @consumedAt
            WHERE token_hash = @tokenHash
              AND consumed_at IS NULL
              AND expires_at > @now
            """;

        var rowsAffected = await _connection.ExecuteAsync(sql, new
        {
            consumedAt = consumedAt.ToString("o"),
            tokenHash,
            now = DateTimeOffset.UtcNow.ToString("o"),
        }, transaction: tx);

        return rowsAffected == 1;
    }

    public async Task RecordConsumerAsync(
        string tokenHash, Guid consumedByUserId, IDbTransaction tx)
    {
        Verify.That(tokenHash).IsNotNull().IsNotEmpty();
        Verify.That(consumedByUserId).IsNotDefault();
        Verify.That(tx).IsNotNull();

        const string sql = """
            UPDATE invitations SET consumed_by_user_id = @consumedByUserId
            WHERE token_hash = @tokenHash
            """;

        var rowsAffected = await _connection.ExecuteAsync(
            sql,
            new { tokenHash, consumedByUserId = consumedByUserId.ToString("D") },
            transaction: tx);

        if (rowsAffected == 0)
            throw new DataAccessException(
                "invite.consumer_not_recorded",
                "RecordConsumerAsync affected 0 rows — invitation record missing after successful TryConsumeAsync");
    }

    public async Task RefreshTokenAsync(Guid id, string newTokenHash, DateTimeOffset newExpiresAt,
                                        IDbTransaction tx)
    {
        Verify.That(id).IsNotDefault();
        Verify.That(newTokenHash).IsNotNull().IsNotEmpty();
        Verify.That(tx).IsNotNull();

        const string sql = """
            UPDATE invitations SET token_hash = @newTokenHash, expires_at = @newExpiresAt
            WHERE id = @id
            """;

        await _connection.ExecuteAsync(
            sql,
            new { id = id.ToString("D"), newTokenHash, newExpiresAt = newExpiresAt.ToString("o") },
            transaction: tx);
    }
}
