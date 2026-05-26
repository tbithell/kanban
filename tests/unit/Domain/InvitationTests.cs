using FluentAssertions;
using Kanban.Domain.Entities;

namespace Kanban.Tests.Unit.Domain;

public class InvitationTests
{
    private static Invitation ValidInvitation(
        DateTimeOffset? expiresAt = null,
        DateTimeOffset? consumedAt = null,
        Guid? consumedByUserId = null) =>
        new(
            Guid.NewGuid(),
            "invitee@example.com",
            Guid.NewGuid(),
            "abc123hash",
            DateTimeOffset.UtcNow.AddHours(-1),
            expiresAt ?? DateTimeOffset.UtcNow.AddDays(7),
            consumedAt,
            consumedByUserId);

    [Fact]
    public void IsExpired_WhenExpiresAtInPast_ReturnsTrue()
    {
        var invitation = ValidInvitation(expiresAt: DateTimeOffset.UtcNow.AddSeconds(-1));
        invitation.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void IsExpired_WhenExpiresAtInFuture_ReturnsFalse()
    {
        var invitation = ValidInvitation(expiresAt: DateTimeOffset.UtcNow.AddDays(7));
        invitation.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void IsConsumed_WhenConsumedAtSet_ReturnsTrue()
    {
        var invitation = ValidInvitation(consumedAt: DateTimeOffset.UtcNow, consumedByUserId: Guid.NewGuid());
        invitation.IsConsumed.Should().BeTrue();
    }

    [Fact]
    public void IsConsumed_WhenNotConsumed_ReturnsFalse()
    {
        var invitation = ValidInvitation();
        invitation.IsConsumed.Should().BeFalse();
    }

    [Fact]
    public void IsRedeemable_WhenNotExpiredAndNotConsumed_ReturnsTrue()
    {
        var invitation = ValidInvitation();
        invitation.IsRedeemable.Should().BeTrue();
    }

    [Fact]
    public void IsRedeemable_WhenExpired_ReturnsFalse()
    {
        var invitation = ValidInvitation(expiresAt: DateTimeOffset.UtcNow.AddSeconds(-1));
        invitation.IsRedeemable.Should().BeFalse();
    }

    [Fact]
    public void IsRedeemable_WhenConsumed_ReturnsFalse()
    {
        var invitation = ValidInvitation(consumedAt: DateTimeOffset.UtcNow, consumedByUserId: Guid.NewGuid());
        invitation.IsRedeemable.Should().BeFalse();
    }

    [Fact]
    public void EmailMatches_IsCaseInsensitive()
    {
        var invitation = ValidInvitation();
        invitation.EmailMatches("INVITEE@EXAMPLE.COM").Should().BeTrue();
        invitation.EmailMatches("invitee@example.com").Should().BeTrue();
        invitation.EmailMatches("InVitEe@ExAmPlE.CoM").Should().BeTrue();
    }

    [Fact]
    public void EmailMatches_WhenDifferentEmail_ReturnsFalse()
    {
        var invitation = ValidInvitation();
        invitation.EmailMatches("other@example.com").Should().BeFalse();
    }

    [Fact]
    public void Consume_SetsConsumedFields()
    {
        var invitation = ValidInvitation();
        var userId = Guid.NewGuid();
        var consumedAt = DateTimeOffset.UtcNow;

        invitation.Consume(userId, consumedAt);

        invitation.ConsumedAt.Should().Be(consumedAt);
        invitation.ConsumedByUserId.Should().Be(userId);
    }

    [Fact]
    public void Consume_WhenUserIdIsEmpty_ThrowsArgumentException()
    {
        var invitation = ValidInvitation();
        Guid userId = Guid.Empty;
        var act = () => invitation.Consume(userId, DateTimeOffset.UtcNow);
        act.Should().Throw<ArgumentException>();
    }
}
