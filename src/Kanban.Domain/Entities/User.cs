using FluentValidation;
using Kanban.Domain.Enums;

namespace Kanban.Domain.Entities;

public sealed class User
{
    public Guid Id { get; }
    public string Email { get; }
    public string DisplayName { get; }
    public SystemRole SystemRole { get; }
    public string? GoogleSub { get; private set; }
    public DateTimeOffset RegisteredAt { get; }
    public DateTimeOffset? LastSignInAt { get; private set; }

    public User(Guid id, string email, string displayName, SystemRole systemRole,
                string? googleSub, DateTimeOffset registeredAt, DateTimeOffset? lastSignInAt)
    {
        new InlineValidator<Guid> { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("id") }.ValidateAndThrow(id);
        new InlineValidator<string> { v => v.RuleFor(x => x).NotEmpty().WithName("email") }.ValidateAndThrow(email);
        new InlineValidator<string> { v => v.RuleFor(x => x).NotEmpty().WithName("displayName") }.ValidateAndThrow(displayName);

        Id = id;
        Email = email;
        DisplayName = displayName;
        SystemRole = systemRole;
        GoogleSub = googleSub;
        RegisteredAt = registeredAt;
        LastSignInAt = lastSignInAt;
    }

    public void LinkGoogleIdentity(string googleSub)
    {
        new InlineValidator<string> { v => v.RuleFor(x => x).NotEmpty().WithName("googleSub") }.ValidateAndThrow(googleSub);
        GoogleSub = googleSub;
    }

    public void RecordSignIn(DateTimeOffset signedInAt) => LastSignInAt = signedInAt;
}
