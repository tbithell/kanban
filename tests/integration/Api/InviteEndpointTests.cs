using System.Data;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dapper;
using FluentAssertions;
using Kanban.Contracts;
using Kanban.Tests.Integration.Infrastructure;

namespace Kanban.Tests.Integration.Api;

public sealed class InviteEndpointTests : IClassFixture<KanbanWebAppFactory>
{
    private readonly KanbanWebAppFactory _factory;

    public InviteEndpointTests(KanbanWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostInvites_WithAdminAndValidEmail_Returns201WithToken()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var client = _factory.CreateAuthenticatedClient(TestPrincipals.RegisteredAdmin(adminId));
        var request = new { email = "newinvitee@example.com" };

        var response = await client.PostAsJsonAsync("/api/v1/invites", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<IssueInviteResponse>();
        body.Should().NotBeNull();
        body!.Token.Should().NotBeNullOrEmpty();
        body.RedemptionLink.Should().Contain("/accept/");
        body.ExpiresAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddDays(7), TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task PostInvites_WithActiveInviteForEmail_Returns200WithToken()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var client = _factory.CreateAuthenticatedClient(TestPrincipals.RegisteredAdmin(adminId));
        var request = new { email = "alreadyinvited@example.com" };

        await _factory.InsertActiveInvitationAsync("alreadyinvited@example.com", adminId);

        var response = await client.PostAsJsonAsync("/api/v1/invites", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<IssueInviteResponse>();
        body!.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task PostInvites_WithRegisteredEmail_Returns409()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var client = _factory.CreateAuthenticatedClient(TestPrincipals.RegisteredAdmin(adminId));
        var request = new { email = KanbanWebAppFactory.AdminEmail };

        var response = await client.PostAsJsonAsync("/api/v1/invites", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("invite.already_registered");
    }

    [Fact]
    public async Task PostInvites_WithInvalidEmail_Returns422()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var client = _factory.CreateAuthenticatedClient(TestPrincipals.RegisteredAdmin(adminId));
        var request = new { email = "not-an-email" };

        var response = await client.PostAsJsonAsync("/api/v1/invites", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task PostInvites_WithNonAdminCaller_Returns403()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var userId = Guid.NewGuid();
        var client = _factory.CreateAuthenticatedClient(
            TestPrincipals.RegisteredStandardUser(userId, "standard@example.com"));
        var request = new { email = "target@example.com" };

        var response = await client.PostAsJsonAsync("/api/v1/invites", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
