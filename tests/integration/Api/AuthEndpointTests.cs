using System.Net;
using FluentAssertions;
using Kanban.Contracts;
using Kanban.Tests.Integration.Infrastructure;
using System.Net.Http.Json;

namespace Kanban.Tests.Integration.Api;

public sealed class AuthEndpointTests : IClassFixture<KanbanWebAppFactory>
{
    private readonly KanbanWebAppFactory _factory;

    public AuthEndpointTests(KanbanWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetMe_WhenRegisteredAdminAuthenticated_Returns200WithCurrentUser()
    {
        var userId = await _factory.GetSeededAdminIdAsync();
        var client = _factory.CreateAuthenticatedClient(TestPrincipals.RegisteredAdmin(userId));

        var response = await client.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<CurrentUserDto>();
        dto.Should().NotBeNull();
        dto!.SystemRole.Should().Be("admin");
    }

    [Fact]
    public async Task GetMe_WhenUnauthenticated_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMe_WhenAuthenticatedButNotRegistered_Returns403WithCode()
    {
        var client = _factory.CreateAuthenticatedClient(TestPrincipals.UnregisteredGoogleUser());

        var response = await client.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("user.not_registered");
    }

    [Fact]
    public async Task Signout_WhenAuthenticated_Returns204()
    {
        var userId = await _factory.GetSeededAdminIdAsync();
        var client = _factory.CreateAuthenticatedClient(
            TestPrincipals.RegisteredAdmin(userId));

        var response = await client.PostAsync("/api/v1/auth/signout", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
