using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Kanban.Contracts;
using Kanban.Tests.Integration.Infrastructure;
using Xunit;

namespace Kanban.Tests.Integration.Api;

public sealed class BoardMemberEndpointTests : IClassFixture<KanbanWebAppFactory>
{
    private readonly KanbanWebAppFactory _factory;

    public BoardMemberEndpointTests(KanbanWebAppFactory factory)
    {
        _factory = factory;
    }

    // ── GET /api/v1/boards/{boardId}/members ───────────────────────────────────

    [Fact]
    public async Task GetMembers_BoardOwner_Returns200WithMembersArray()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var boardId = await _factory.InsertBoardAsync("Members List Board", adminId);
        var memberId = await _factory.InsertStandardUserAsync("list-member@test.local");
        await _factory.InsertBoardMemberAsync(boardId, memberId, "Member");
        var client = _factory.CreateAuthenticatedClient(TestPrincipals.RegisteredAdmin(adminId));

        var response = await client.GetAsync($"/api/v1/boards/{boardId}/members");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.ValueKind.Should().Be(JsonValueKind.Array);
        body.GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetMembers_NonMemberCaller_Returns404()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var boardId = await _factory.InsertBoardAsync("Private Members Board", adminId);
        var outsiderId = await _factory.InsertStandardUserAsync("outsider-mem@test.local");
        var client = _factory.CreateAuthenticatedClient(
            TestPrincipals.RegisteredStandardUser(outsiderId, "outsider-mem@test.local"));

        var response = await client.GetAsync($"/api/v1/boards/{boardId}/members");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMembers_MemberCaller_Returns200()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var boardId = await _factory.InsertBoardAsync("Member View Board", adminId);
        var memberId = await _factory.InsertStandardUserAsync("view-member@test.local");
        await _factory.InsertBoardMemberAsync(boardId, memberId, "Member");
        var client = _factory.CreateAuthenticatedClient(
            TestPrincipals.RegisteredStandardUser(memberId, "view-member@test.local"));

        var response = await client.GetAsync($"/api/v1/boards/{boardId}/members");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── POST /api/v1/boards/{boardId}/members/invite ─────────────────────────

    [Fact]
    public async Task InviteMember_OwnerInvitesNewEmail_Returns202()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var boardId = await _factory.InsertBoardAsync("Invite Board", adminId);
        var client = _factory.CreateAuthenticatedClient(TestPrincipals.RegisteredAdmin(adminId));

        var response = await client.PostAsJsonAsync(
            $"/api/v1/boards/{boardId}/members/invite",
            new { email = $"new-invite-{Guid.NewGuid():N}@test.local", role = "Member" });

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task InviteMember_MemberCallerRole_Returns403()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var boardId = await _factory.InsertBoardAsync("Member Invite Board", adminId);
        var memberId = await _factory.InsertStandardUserAsync("member-invite@test.local");
        await _factory.InsertBoardMemberAsync(boardId, memberId, "Member");
        var client = _factory.CreateAuthenticatedClient(
            TestPrincipals.RegisteredStandardUser(memberId, "member-invite@test.local"));

        var response = await client.PostAsJsonAsync(
            $"/api/v1/boards/{boardId}/members/invite",
            new { email = "victim@test.local", role = "Member" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task InviteMember_NonMemberCaller_Returns404()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var boardId = await _factory.InsertBoardAsync("Invite 404 Board", adminId);
        var outsiderId = await _factory.InsertStandardUserAsync("outsider-invite@test.local");
        var client = _factory.CreateAuthenticatedClient(
            TestPrincipals.RegisteredStandardUser(outsiderId, "outsider-invite@test.local"));

        var response = await client.PostAsJsonAsync(
            $"/api/v1/boards/{boardId}/members/invite",
            new { email = "someone@test.local", role = "Member" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── PATCH /api/v1/boards/{boardId}/members/{userId}/role ─────────────────

    [Fact]
    public async Task ChangeRole_OwnerChangesRole_Returns200WithUpdatedMember()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var boardId = await _factory.InsertBoardAsync("Change Role Board", adminId);
        var targetId = await _factory.InsertStandardUserAsync("target-role@test.local");
        await _factory.InsertBoardMemberAsync(boardId, targetId, "Member");
        var client = _factory.CreateAuthenticatedClient(TestPrincipals.RegisteredAdmin(adminId));

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/boards/{boardId}/members/{targetId}/role",
            new { role = "Viewer" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<BoardMemberDto>();
        body!.Role.Should().Be(BoardRoleDto.Viewer);
        body.UserId.Should().Be(targetId);
    }

    [Fact]
    public async Task ChangeRole_LastOwnerDowngrade_Returns422()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var boardId = await _factory.InsertBoardAsync("Last Owner Board", adminId);
        var client = _factory.CreateAuthenticatedClient(TestPrincipals.RegisteredAdmin(adminId));

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/boards/{boardId}/members/{adminId}/role",
            new { role = "Member" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ChangeRole_MemberCallerRole_Returns403()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var boardId = await _factory.InsertBoardAsync("Member Change Board", adminId);
        var memberId = await _factory.InsertStandardUserAsync("member-change@test.local");
        await _factory.InsertBoardMemberAsync(boardId, memberId, "Member");
        var client = _factory.CreateAuthenticatedClient(
            TestPrincipals.RegisteredStandardUser(memberId, "member-change@test.local"));

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/boards/{boardId}/members/{adminId}/role",
            new { role = "Viewer" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── DELETE /api/v1/boards/{boardId}/members/{userId} ─────────────────────

    [Fact]
    public async Task RemoveMember_OwnerRemovesMember_Returns204()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var boardId = await _factory.InsertBoardAsync("Remove Member Board", adminId);
        var targetId = await _factory.InsertStandardUserAsync("remove-target@test.local");
        await _factory.InsertBoardMemberAsync(boardId, targetId, "Member");
        var client = _factory.CreateAuthenticatedClient(TestPrincipals.RegisteredAdmin(adminId));

        var response = await client.DeleteAsync($"/api/v1/boards/{boardId}/members/{targetId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RemoveMember_RemoveLastOwner_Returns422()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var boardId = await _factory.InsertBoardAsync("Remove Last Owner Board", adminId);
        var client = _factory.CreateAuthenticatedClient(TestPrincipals.RegisteredAdmin(adminId));

        var response = await client.DeleteAsync($"/api/v1/boards/{boardId}/members/{adminId}");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task RemoveMember_MemberCallerRole_Returns403()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var boardId = await _factory.InsertBoardAsync("Remove Auth Board", adminId);
        var memberId = await _factory.InsertStandardUserAsync("remove-member-caller@test.local");
        await _factory.InsertBoardMemberAsync(boardId, memberId, "Member");
        var client = _factory.CreateAuthenticatedClient(
            TestPrincipals.RegisteredStandardUser(memberId, "remove-member-caller@test.local"));

        var response = await client.DeleteAsync($"/api/v1/boards/{boardId}/members/{adminId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
