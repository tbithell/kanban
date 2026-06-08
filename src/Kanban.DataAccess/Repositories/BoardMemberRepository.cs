using System.Data;
using Dapper;
using FluentValidation;
using Kanban.DataAccess.Interfaces;
using Kanban.Domain;
using Kanban.Domain.Entities;
using Kanban.Domain.Enums;

namespace Kanban.DataAccess.Repositories;

public sealed class BoardMemberRepository : IBoardMemberRepository
{
    private readonly IDbConnection _connection;

    public BoardMemberRepository(IDbConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
    }

    public async Task<BoardRole?> FindRoleAsync(Guid boardId, Guid userId, IDbTransaction? tx = null)
    {
        new InlineValidator<Guid> { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("boardId") }.ValidateAndThrow(boardId);
        new InlineValidator<Guid> { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("userId") }.ValidateAndThrow(userId);

        const string sql = """
            SELECT role FROM board_members
            WHERE board_id = @boardId AND user_id = @userId
            """;

        var roleString = await _connection.QuerySingleOrDefaultAsync<string?>(
            sql, new { boardId = boardId.ToString("D"), userId = userId.ToString("D") }, transaction: tx);

        return roleString is null ? null : Enum.Parse<BoardRole>(roleString);
    }

    public async Task<int> CountOwnersAsync(Guid boardId, IDbTransaction? tx = null)
    {
        new InlineValidator<Guid> { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("boardId") }.ValidateAndThrow(boardId);

        const string sql = """
            SELECT COUNT(*) FROM board_members
            WHERE board_id = @boardId AND role = 'Owner'
            """;

        return await _connection.QuerySingleAsync<int>(
            sql, new { boardId = boardId.ToString("D") }, transaction: tx);
    }

    public async Task InsertAsync(BoardMember member, IDbTransaction tx)
    {
        ArgumentNullException.ThrowIfNull(member);
        ArgumentNullException.ThrowIfNull(tx);

        const string sql = """
            INSERT INTO board_members (id, board_id, user_id, role, invited_by_user_id, joined_at)
            VALUES (@id, @boardId, @userId, @role, @invitedByUserId, @joinedAt)
            """;

        await _connection.ExecuteAsync(sql, new
        {
            id = member.Id.ToString("D"),
            boardId = member.BoardId.ToString("D"),
            userId = member.UserId.ToString("D"),
            role = member.Role.ToString(),
            invitedByUserId = member.InvitedByUserId?.ToString("D"),
            joinedAt = member.JoinedAt.ToString("o"),
        }, transaction: tx);
    }

    public async Task DeleteAsync(Guid boardId, Guid userId, IDbTransaction tx)
    {
        new InlineValidator<Guid> { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("boardId") }.ValidateAndThrow(boardId);
        new InlineValidator<Guid> { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("userId") }.ValidateAndThrow(userId);
        ArgumentNullException.ThrowIfNull(tx);

        const string sql = """
            DELETE FROM board_members
            WHERE board_id = @boardId AND user_id = @userId
            """;

        await _connection.ExecuteAsync(sql,
            new { boardId = boardId.ToString("D"), userId = userId.ToString("D") }, transaction: tx);
    }

    public async Task UpdateRoleAsync(Guid boardId, Guid userId, BoardRole newRole, IDbTransaction tx)
    {
        new InlineValidator<Guid> { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("boardId") }.ValidateAndThrow(boardId);
        new InlineValidator<Guid> { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("userId") }.ValidateAndThrow(userId);
        ArgumentNullException.ThrowIfNull(tx);

        const string sql = """
            UPDATE board_members SET role = @role
            WHERE board_id = @boardId AND user_id = @userId
            """;

        await _connection.ExecuteAsync(sql, new
        {
            boardId = boardId.ToString("D"),
            userId = userId.ToString("D"),
            role = newRole.ToString(),
        }, transaction: tx);
    }

    public async Task<IReadOnlyList<BoardMember>> FindAllForBoardAsync(Guid boardId, IDbTransaction? tx = null)
    {
        new InlineValidator<Guid> { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("boardId") }.ValidateAndThrow(boardId);

        const string sql = """
            SELECT id, board_id AS BoardId, user_id AS UserId, role AS Role,
                   invited_by_user_id AS InvitedByUserId, joined_at AS JoinedAt
            FROM board_members
            WHERE board_id = @boardId
            ORDER BY joined_at ASC
            """;

        var results = await _connection.QueryAsync<BoardMemberRow>(
            sql, new { boardId = boardId.ToString("D") }, transaction: tx);

        return results.Select(r => new BoardMember(
            id: r.Id,
            boardId: r.BoardId,
            userId: r.UserId,
            role: Enum.Parse<BoardRole>(r.Role),
            invitedByUserId: r.InvitedByUserId,
            joinedAt: r.JoinedAt)).ToList();
    }

    private sealed class BoardMemberRow
    {
        public Guid Id { get; init; }
        public Guid BoardId { get; init; }
        public Guid UserId { get; init; }
        public string Role { get; init; } = string.Empty;
        public Guid? InvitedByUserId { get; init; }
        public DateTimeOffset JoinedAt { get; init; }
    }
}
