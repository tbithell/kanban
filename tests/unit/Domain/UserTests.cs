using FluentAssertions;
using FluentValidation;
using Kanban.Domain.Entities;
using Kanban.Domain.Enums;

namespace Kanban.Tests.Unit.Domain;

public class UserTests
{
    private static User ValidUser() =>
        new(Guid.NewGuid(), "admin@example.com", "Admin User", SystemRole.Admin,
            null, DateTimeOffset.UtcNow, null);

    [Fact]
    public void Constructor_WhenIdIsEmpty_ThrowsValidationException()
    {
        Guid id = Guid.Empty;
        var act = () => new User(id, "a@b.com", "Name", SystemRole.Standard,
            null, DateTimeOffset.UtcNow, null);
        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void Constructor_WhenEmailIsEmpty_ThrowsValidationException()
    {
        string email = string.Empty;
        var act = () => new User(Guid.NewGuid(), email, "Name", SystemRole.Standard,
            null, DateTimeOffset.UtcNow, null);
        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void Constructor_WhenDisplayNameIsEmpty_ThrowsValidationException()
    {
        string displayName = string.Empty;
        var act = () => new User(Guid.NewGuid(), "a@b.com", displayName, SystemRole.Standard,
            null, DateTimeOffset.UtcNow, null);
        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void LinkGoogleIdentity_SetsGoogleSub()
    {
        var user = ValidUser();
        string googleSub = "google-sub-123";
        user.LinkGoogleIdentity(googleSub);
        user.GoogleSub.Should().Be("google-sub-123");
    }

    [Fact]
    public void LinkGoogleIdentity_WhenSubIsEmpty_ThrowsValidationException()
    {
        var user = ValidUser();
        string googleSub = string.Empty;
        var act = () => user.LinkGoogleIdentity(googleSub);
        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void RecordSignIn_SetsLastSignInAt()
    {
        var user = ValidUser();
        var signedInAt = DateTimeOffset.UtcNow;
        user.RecordSignIn(signedInAt);
        user.LastSignInAt.Should().Be(signedInAt);
    }
}
