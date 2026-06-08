using FluentAssertions;
using FluentValidation;
using Kanban.Domain.ValueObjects;

namespace Kanban.Tests.Unit.Domain;

public class InvitationTokenTests
{
    [Fact]
    public void Generate_ProducesUrlSafeBase64_Of43Chars()
    {
        var token = InvitationToken.Generate();

        token.Raw.Should().HaveLength(43);
        token.Raw.Should().MatchRegex(@"^[A-Za-z0-9\-_]+$");
    }

    [Fact]
    public void Generate_ProducesLowercaseHexHash()
    {
        var token = InvitationToken.Generate();

        token.Hash.Should().MatchRegex(@"^[0-9a-f]{64}$");
    }

    [Fact]
    public void Generate_TwoCallsProduceDifferentTokens()
    {
        var token1 = InvitationToken.Generate();
        var token2 = InvitationToken.Generate();

        token1.Raw.Should().NotBe(token2.Raw);
        token1.Hash.Should().NotBe(token2.Hash);
    }

    [Fact]
    public void HashRaw_SameInputAlwaysProducesSameHash()
    {
        string rawToken = "some-raw-token-value-for-test";
        var hash1 = InvitationToken.HashRaw(rawToken);
        var hash2 = InvitationToken.HashRaw(rawToken);

        hash1.Should().Be(hash2);
        hash1.Should().MatchRegex(@"^[0-9a-f]{64}$");
    }

    [Fact]
    public void HashRaw_WhenEmpty_ThrowsValidationException()
    {
        string rawToken = string.Empty;
        var act = () => InvitationToken.HashRaw(rawToken);
        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void Generate_HashMatchesHashRaw()
    {
        var token = InvitationToken.Generate();
        var recomputed = InvitationToken.HashRaw(token.Raw);
        recomputed.Should().Be(token.Hash);
    }
}
