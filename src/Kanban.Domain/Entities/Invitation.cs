using FluentValidation;
using Kanban.Domain.Enums;

namespace Kanban.Domain.Entities;

public sealed class Invitation
{
    public Guid Id { get; }
    public string Email { get; }
    public Guid IssuedByUserId { get; }
    public string TokenHash { get; }
    public DateTimeOffset IssuedAt { get; }
    public DateTimeOffset ExpiresAt { get; }
    public DateTimeOffset? ConsumedAt { get; private set; }
    public Guid? ConsumedByUserId { get; private set; }
    public Guid? BoardId { get; init; }
    public BoardRole? BoardRole { get; init; }

    public bool IsExpired => DateTimeOffset.UtcNow > ExpiresAt;
    public bool IsConsumed => ConsumedAt.HasValue;
    public bool IsRedeemable => !IsExpired && !IsConsumed;

    public Invitation(Guid id, string email, Guid issuedByUserId, string tokenHash,
                      DateTimeOffset issuedAt, DateTimeOffset expiresAt,
                      DateTimeOffset? consumedAt, Guid? consumedByUserId,
                      Guid? boardId = null, BoardRole? boardRole = null)
    {
        new InlineValidator<Guid> { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("id") }.ValidateAndThrow(id);
        new InlineValidator<string> { v => v.RuleFor(x => x).NotEmpty().WithName("email") }.ValidateAndThrow(email);
        new InlineValidator<Guid> { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("issuedByUserId") }.ValidateAndThrow(issuedByUserId);
        new InlineValidator<string> { v => v.RuleFor(x => x).NotEmpty().WithName("tokenHash") }.ValidateAndThrow(tokenHash);

        Id = id;
        Email = email;
        IssuedByUserId = issuedByUserId;
        TokenHash = tokenHash;
        IssuedAt = issuedAt;
        ExpiresAt = expiresAt;
        ConsumedAt = consumedAt;
        ConsumedByUserId = consumedByUserId;
        BoardId = boardId;
        BoardRole = boardRole;
    }

    public bool EmailMatches(string googleEmail)
    {
        new InlineValidator<string> { v => v.RuleFor(x => x).NotEmpty().WithName("googleEmail") }.ValidateAndThrow(googleEmail);
        return string.Equals(Email, googleEmail, StringComparison.OrdinalIgnoreCase);
    }

    public void Consume(Guid userId, DateTimeOffset consumedAt)
    {
        new InlineValidator<Guid> { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("userId") }.ValidateAndThrow(userId);
        ConsumedAt = consumedAt;
        ConsumedByUserId = userId;
    }
}
