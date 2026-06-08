using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Kanban.Contracts;
using Kanban.Tests.Integration.Infrastructure;
using Xunit;

namespace Kanban.Tests.Integration.Api;

public sealed class BoardEndpointTests : IClassFixture<KanbanWebAppFactory>
{
    private readonly KanbanWebAppFactory _factory;

    public BoardEndpointTests(KanbanWebAppFactory factory)
    {
        _factory = factory;
    }

    // ── GET /api/v1/boards ────────────────────────────────────────────────────

    [Fact]
    public async Task GetBoards_AdminWithNoBoards_ReturnsEmptyArray()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var client = _factory.CreateAuthenticatedClient(TestPrincipals.RegisteredAdmin(adminId));

        var response = await client.GetAsync("/api/v1/boards");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GetBoards_MemberReturnsOwnBoards_NotOtherUsersBoards()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var boardId = await _factory.InsertBoardAsync("Admin Board", adminId);
        var memberId = await _factory.InsertStandardUserAsync("member@test.local");
        await _factory.InsertBoardMemberAsync(boardId, memberId, "Member");
        var memberClient = _factory.CreateAuthenticatedClient(
            TestPrincipals.RegisteredStandardUser(memberId, "member@test.local"));
        var adminClient = _factory.CreateAuthenticatedClient(TestPrincipals.RegisteredAdmin(adminId));

        var memberResp = await memberClient.GetAsync("/api/v1/boards");
        var adminResp = await adminClient.GetAsync("/api/v1/boards");

        memberResp.StatusCode.Should().Be(HttpStatusCode.OK);
        adminResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetBoards_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/boards");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── POST /api/v1/boards ───────────────────────────────────────────────────

    [Fact]
    public async Task PostBoards_AdminWithValidName_Returns201WithBoardDetail()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var client = _factory.CreateAuthenticatedClient(TestPrincipals.RegisteredAdmin(adminId));
        var request = new { name = $"Board-{Guid.NewGuid():N}" };

        var response = await client.PostAsJsonAsync("/api/v1/boards", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<BoardDetailDto>();
        body.Should().NotBeNull();
        body!.Name.Should().Be(request.name);
        body.CallerRole.Should().Be(BoardRoleDto.Owner);
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task PostBoards_NonAdmin_Returns403()
    {
        var memberId = await _factory.InsertStandardUserAsync("nonAdmin@test.local");
        var client = _factory.CreateAuthenticatedClient(
            TestPrincipals.RegisteredStandardUser(memberId, "nonAdmin@test.local"));

        var response = await client.PostAsJsonAsync("/api/v1/boards", new { name = "Forbidden" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PostBoards_DuplicateName_Returns409()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        await _factory.InsertBoardAsync("Duplicate Board", adminId);
        var client = _factory.CreateAuthenticatedClient(TestPrincipals.RegisteredAdmin(adminId));

        var response = await client.PostAsJsonAsync("/api/v1/boards", new { name = "Duplicate Board" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("board.name_conflict");
    }

    [Fact]
    public async Task PostBoards_MissingName_Returns422()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var client = _factory.CreateAuthenticatedClient(TestPrincipals.RegisteredAdmin(adminId));

        var response = await client.PostAsJsonAsync("/api/v1/boards", new { name = "" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ── GET /api/v1/boards/{id} ───────────────────────────────────────────────

    [Fact]
    public async Task GetBoard_MemberCaller_Returns200WithDetail()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var boardId = await _factory.InsertBoardAsync("Detail Board", adminId);
        var client = _factory.CreateAuthenticatedClient(TestPrincipals.RegisteredAdmin(adminId));

        var response = await client.GetAsync($"/api/v1/boards/{boardId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<BoardDetailDto>();
        body!.Id.Should().Be(boardId);
        body.Name.Should().Be("Detail Board");
    }

    [Fact]
    public async Task GetBoard_NonMemberCaller_Returns404()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var boardId = await _factory.InsertBoardAsync("Private Board", adminId);
        var nonMemberId = await _factory.InsertStandardUserAsync("nonMember@test.local");
        var client = _factory.CreateAuthenticatedClient(
            TestPrincipals.RegisteredStandardUser(nonMemberId, "nonMember@test.local"));

        var response = await client.GetAsync($"/api/v1/boards/{boardId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── DELETE /api/v1/boards/{id} ────────────────────────────────────────────

    [Fact]
    public async Task DeleteBoard_OwnerCaller_Returns204()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var boardId = await _factory.InsertBoardAsync("Delete Board", adminId);
        var client = _factory.CreateAuthenticatedClient(TestPrincipals.RegisteredAdmin(adminId));

        var response = await client.DeleteAsync($"/api/v1/boards/{boardId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteBoard_MemberNotOwner_Returns403()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var boardId = await _factory.InsertBoardAsync("Protected Board", adminId);
        var memberId = await _factory.InsertStandardUserAsync("member2@test.local");
        await _factory.InsertBoardMemberAsync(boardId, memberId, "Member");
        var client = _factory.CreateAuthenticatedClient(
            TestPrincipals.RegisteredStandardUser(memberId, "member2@test.local"));

        var response = await client.DeleteAsync($"/api/v1/boards/{boardId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteBoard_NonMemberCaller_Returns404()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var boardId = await _factory.InsertBoardAsync("Invisible Board", adminId);
        var nonMemberId = await _factory.InsertStandardUserAsync("ghost@test.local");
        var client = _factory.CreateAuthenticatedClient(
            TestPrincipals.RegisteredStandardUser(nonMemberId, "ghost@test.local"));

        var response = await client.DeleteAsync($"/api/v1/boards/{boardId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
