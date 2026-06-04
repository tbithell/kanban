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

    // ── POST /api/v1/invites ──────────────────────────────────────────────────

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

    // ── POST /api/v1/invites/{token}/accept ───────────────────────────────────

    [Fact]
    public async Task PostAcceptInvite_ValidToken_Returns200WithCurrentUserDto()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var email = "validacceptee@example.com";
        var rawToken = await _factory.InsertInvitationWithRawTokenAsync(email, adminId);
        var client = _factory.CreateAuthenticatedClient(
            TestPrincipals.UnregisteredGoogleUser(email));

        var response = await client.PostAsync($"/api/v1/invites/{rawToken}/accept", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CurrentUserDto>();
        body.Should().NotBeNull();
        body!.Email.Should().Be(email);
        body.SystemRole.Should().Be("standard");
    }

    [Fact]
    public async Task PostAcceptInvite_ExpiredToken_Returns410()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var email = "expiredacceptee@example.com";
        var rawToken = await _factory.InsertInvitationWithRawTokenAsync(
            email, adminId, expiresAt: DateTimeOffset.UtcNow.AddDays(-1));
        var client = _factory.CreateAuthenticatedClient(
            TestPrincipals.UnregisteredGoogleUser(email));

        var response = await client.PostAsync($"/api/v1/invites/{rawToken}/accept", null);

        response.StatusCode.Should().Be(HttpStatusCode.Gone);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("invite.invalid");
    }

    [Fact]
    public async Task PostAcceptInvite_ConsumedToken_Returns410()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var email = "consumedacceptee@example.com";
        var rawToken = await _factory.InsertInvitationWithRawTokenAsync(
            email, adminId, consumedAt: DateTimeOffset.UtcNow.AddMinutes(-5));
        var client = _factory.CreateAuthenticatedClient(
            TestPrincipals.UnregisteredGoogleUser(email));

        var response = await client.PostAsync($"/api/v1/invites/{rawToken}/accept", null);

        response.StatusCode.Should().Be(HttpStatusCode.Gone);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("invite.invalid");
    }

    [Fact]
    public async Task PostAcceptInvite_NonExistentToken_Returns410()
    {
        var client = _factory.CreateAuthenticatedClient(
            TestPrincipals.UnregisteredGoogleUser("anyone@example.com"));
        var fabricatedToken = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

        var response = await client.PostAsync($"/api/v1/invites/{fabricatedToken}/accept", null);

        response.StatusCode.Should().Be(HttpStatusCode.Gone);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("invite.invalid");
    }

    [Fact]
    public async Task PostAcceptInvite_InvalidTokenVariants_ReturnIndistinguishableResponses()
    {
        // SC-005: expired, consumed, and fabricated tokens must be indistinguishable to the caller.
        // All three must return the same HTTP status and the same error code so that an attacker
        // cannot determine whether a token ever existed or whether it has been used.
        var adminId = await _factory.GetSeededAdminIdAsync();

        var expiredToken = await _factory.InsertInvitationWithRawTokenAsync(
            "sc005-expired@example.com", adminId, expiresAt: DateTimeOffset.UtcNow.AddDays(-1));
        var consumedToken = await _factory.InsertInvitationWithRawTokenAsync(
            "sc005-consumed@example.com", adminId, consumedAt: DateTimeOffset.UtcNow.AddMinutes(-5));
        const string fabricatedToken = "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB";

        var expiredClient = _factory.CreateAuthenticatedClient(
            TestPrincipals.UnregisteredGoogleUser("sc005-expired@example.com"));
        var consumedClient = _factory.CreateAuthenticatedClient(
            TestPrincipals.UnregisteredGoogleUser("sc005-consumed@example.com"));
        var fabricatedClient = _factory.CreateAuthenticatedClient(
            TestPrincipals.UnregisteredGoogleUser("sc005-fabricated@example.com"));

        var expiredResponse = await expiredClient.PostAsync($"/api/v1/invites/{expiredToken}/accept", null);
        var consumedResponse = await consumedClient.PostAsync($"/api/v1/invites/{consumedToken}/accept", null);
        var fabricatedResponse = await fabricatedClient.PostAsync($"/api/v1/invites/{fabricatedToken}/accept", null);

        expiredResponse.StatusCode.Should().Be(HttpStatusCode.Gone);
        consumedResponse.StatusCode.Should().Be(HttpStatusCode.Gone);
        fabricatedResponse.StatusCode.Should().Be(HttpStatusCode.Gone);

        var expiredBody = await expiredResponse.Content.ReadAsStringAsync();
        var consumedBody = await consumedResponse.Content.ReadAsStringAsync();
        var fabricatedBody = await fabricatedResponse.Content.ReadAsStringAsync();

        // All three must surface the same opaque error code — no hint about token lifecycle state.
        expiredBody.Should().Contain("invite.invalid");
        consumedBody.Should().Contain("invite.invalid");
        fabricatedBody.Should().Contain("invite.invalid");

        // None of the bodies should reveal which specific failure path was triggered.
        expiredBody.Should().NotContain("expired");
        consumedBody.Should().NotContain("consumed");
        fabricatedBody.Should().NotContain("fabricated");
    }

    [Fact]
    public async Task PostAcceptInvite_EmailMismatch_Returns422()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var inviteeEmail = "mismatchinvitee@example.com";
        var rawToken = await _factory.InsertInvitationWithRawTokenAsync(inviteeEmail, adminId);
        var client = _factory.CreateAuthenticatedClient(
            TestPrincipals.UnregisteredGoogleUser("differentemail@example.com"));

        var response = await client.PostAsync($"/api/v1/invites/{rawToken}/accept", null);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("invite.email_mismatch");
    }

    [Fact]
    public async Task PostAcceptInvite_ConcurrentRequests_Exactly1Returns200AndNoDuplicateUsers()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var email = "concurrentacceptee@example.com";
        var rawToken = await _factory.InsertInvitationWithRawTokenAsync(email, adminId);

        // 5 concurrent requests is sufficient to prove the single-winner invariant without
        // fighting SQLite's single-writer constraint. Load testing belongs in a dedicated
        // suite that documents its infrastructure assumptions.
        const int concurrency = 5;
        var tasks = Enumerable.Range(0, concurrency).Select(_ =>
        {
            var client = _factory.CreateAuthenticatedClient(
                TestPrincipals.UnregisteredGoogleUser(email));
            return client.PostAsync($"/api/v1/invites/{rawToken}/accept", null);
        }).ToList();

        var responses = await Task.WhenAll(tasks);
        var statusCodes = responses.Select(r => r.StatusCode).ToList();

        var grouped = statusCodes.GroupBy(c => c)
            .Select(g => $"{g.Key}:{g.Count()}")
            .OrderBy(s => s);
        statusCodes.Count(c => c == HttpStatusCode.OK).Should()
            .Be(1, $"status codes were: {string.Join(", ", grouped)}");
        statusCodes.Count(c => c == HttpStatusCode.Gone).Should()
            .Be(concurrency - 1, $"status codes were: {string.Join(", ", grouped)}");

        var userCount = await _factory.CountUsersForEmailAsync(email);
        userCount.Should().Be(1);
    }

}
