using System.Data;
using Dapper;
using FluentValidation;
using Kanban.DataAccess.Interfaces;
using Kanban.Domain;
using Kanban.Domain.Entities;
using Kanban.Domain.Enums;

namespace Kanban.DataAccess.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly IDbConnection _connection;

    public UserRepository(IDbConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
    }

    public async Task<User?> FindByGoogleSubAsync(string googleSub, IDbTransaction? tx = null)
    {
        new InlineValidator<string> { v => v.RuleFor(x => x).NotEmpty().WithName("googleSub") }.ValidateAndThrow(googleSub);

        const string sql = """
            SELECT id, email, display_name AS DisplayName, system_role AS SystemRole,
                   google_sub AS GoogleSub, registered_at AS RegisteredAt,
                   last_sign_in_at AS LastSignInAt
            FROM users
            WHERE google_sub = @googleSub
            """;

        return await _connection.QuerySingleOrDefaultAsync<User>(
            sql, new { googleSub }, transaction: tx);
    }

    public async Task<User?> FindByEmailAsync(string email, IDbTransaction? tx = null)
    {
        new InlineValidator<string> { v => v.RuleFor(x => x).NotEmpty().WithName("email") }.ValidateAndThrow(email);

        const string sql = """
            SELECT id, email, display_name AS DisplayName, system_role AS SystemRole,
                   google_sub AS GoogleSub, registered_at AS RegisteredAt,
                   last_sign_in_at AS LastSignInAt
            FROM users
            WHERE email = @email
            """;

        return await _connection.QuerySingleOrDefaultAsync<User>(
            sql, new { email }, transaction: tx);
    }

    public async Task<User?> FindByIdAsync(Guid id, IDbTransaction? tx = null)
    {
        new InlineValidator<Guid> { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("id") }.ValidateAndThrow(id);

        const string sql = """
            SELECT id, email, display_name AS DisplayName, system_role AS SystemRole,
                   google_sub AS GoogleSub, registered_at AS RegisteredAt,
                   last_sign_in_at AS LastSignInAt
            FROM users
            WHERE id = @id
            """;

        return await _connection.QuerySingleOrDefaultAsync<User>(
            sql, new { id = id.ToString("D") }, transaction: tx);
    }

    public async Task InsertAsync(User user, IDbTransaction tx)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(tx);

        const string sql = """
            INSERT INTO users (id, email, display_name, system_role, google_sub, registered_at, last_sign_in_at)
            VALUES (@id, @email, @displayName, @systemRole, @googleSub, @registeredAt, @lastSignInAt)
            """;

        await _connection.ExecuteAsync(sql, new
        {
            id = user.Id.ToString("D"),
            email = user.Email,
            displayName = user.DisplayName,
            systemRole = user.SystemRole.ToString(),
            googleSub = user.GoogleSub,
            registeredAt = user.RegisteredAt.ToString("o"),
            lastSignInAt = user.LastSignInAt?.ToString("o"),
        }, transaction: tx);
    }

    public async Task LinkGoogleSubAsync(Guid userId, string googleSub, IDbTransaction tx)
    {
        new InlineValidator<Guid> { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("userId") }.ValidateAndThrow(userId);
        new InlineValidator<string> { v => v.RuleFor(x => x).NotEmpty().WithName("googleSub") }.ValidateAndThrow(googleSub);
        ArgumentNullException.ThrowIfNull(tx);

        const string sql = """
            UPDATE users SET google_sub = @googleSub WHERE id = @userId
            """;

        await _connection.ExecuteAsync(
            sql,
            new { userId = userId.ToString("D"), googleSub },
            transaction: tx);
    }

    public async Task UpdateLastSignInAsync(Guid userId, DateTimeOffset signedInAt, IDbTransaction tx)
    {
        new InlineValidator<Guid> { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("userId") }.ValidateAndThrow(userId);
        ArgumentNullException.ThrowIfNull(tx);

        const string sql = """
            UPDATE users SET last_sign_in_at = @signedInAt WHERE id = @userId
            """;

        await _connection.ExecuteAsync(
            sql,
            new { userId = userId.ToString("D"), signedInAt = signedInAt.ToString("o") },
            transaction: tx);
    }
}
