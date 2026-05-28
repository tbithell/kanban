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
        Verify.That(id).IsNotDefault();
        Verify.That(email).IsNotNull().IsNotEmpty();
        Verify.That(displayName).IsNotNull().IsNotEmpty();

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
        Verify.That(googleSub).IsNotNull().IsNotEmpty();
        GoogleSub = googleSub;
    }

    public void RecordSignIn(DateTimeOffset signedInAt) => LastSignInAt = signedInAt;
}
