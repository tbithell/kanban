using Kanban.Domain.Entities;
using Kanban.Domain.Enums;

namespace Kanban.Tests.Unit.Builders;

public sealed class UserBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _email = $"user_{Guid.NewGuid():N}@test.local";
    private string _displayName = "Test User";
    private SystemRole _systemRole = SystemRole.Standard;
    private string? _googleSub = $"sub_{Guid.NewGuid():N}";
    private DateTimeOffset _registeredAt = DateTimeOffset.UtcNow;
    private DateTimeOffset? _lastSignInAt;

    public static UserBuilder AUser() => new();

    public UserBuilder WithId(Guid id) { _id = id; return this; }
    public UserBuilder WithEmail(string email) { _email = email; return this; }
    public UserBuilder WithDisplayName(string displayName) { _displayName = displayName; return this; }
    public UserBuilder WithSystemRole(SystemRole role) { _systemRole = role; return this; }
    public UserBuilder WithGoogleSub(string? googleSub) { _googleSub = googleSub; return this; }
    public UserBuilder WithoutGoogleSub() { _googleSub = null; return this; }
    public UserBuilder RegisteredAt(DateTimeOffset registeredAt) { _registeredAt = registeredAt; return this; }
    public UserBuilder LastSignedInAt(DateTimeOffset lastSignInAt) { _lastSignInAt = lastSignInAt; return this; }

    public UserBuilder AsAdmin() { _systemRole = SystemRole.Admin; return this; }

    public User Build() =>
        new(
            id: _id,
            email: _email,
            displayName: _displayName,
            systemRole: _systemRole,
            googleSub: _googleSub,
            registeredAt: _registeredAt,
            lastSignInAt: _lastSignInAt);
}
