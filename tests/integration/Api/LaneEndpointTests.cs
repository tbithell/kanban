using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Kanban.Contracts;
using Kanban.Tests.Integration.Infrastructure;
using Xunit;

namespace Kanban.Tests.Integration.Api;

public sealed class LaneEndpointTests : IClassFixture<KanbanWebAppFactory>
{
    private readonly KanbanWebAppFactory _factory;

    public LaneEndpointTests(KanbanWebAppFactory factory)
    {
        _factory = factory;
    }

    // ── POST /api/v1/boards/{boardId}/lanes ───────────────────────────────────

    [Fact]
    public async Task PostLane_OwnerWithValidName_Returns201WithLaneDto()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var boardId = await _factory.InsertBoardAsync("Lane Board", adminId);
        var client = _factory.CreateAuthenticatedClient(TestPrincipals.RegisteredAdmin(adminId));
        var request = new { name = "To Do" };

        var response = await client.PostAsJsonAsync($"/api/v1/boards/{boardId}/lanes", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<LaneDto>();
        body.Should().NotBeNull();
        body!.Name.Should().Be("To Do");
        body.Position.Should().Be(1);
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task PostLane_SecondLane_AppendsAtPositionTwo()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var boardId = await _factory.InsertBoardAsync("Two Lane Board", adminId);
        await _factory.InsertLaneAsync(boardId, "Lane 1", 1);
        var client = _factory.CreateAuthenticatedClient(TestPrincipals.RegisteredAdmin(adminId));

        var response = await client.PostAsJsonAsync($"/api/v1/boards/{boardId}/lanes", new { name = "Lane 2" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<LaneDto>();
        body!.Position.Should().Be(2);
    }

    [Fact]
    public async Task PostLane_ViewerCaller_Returns403()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var boardId = await _factory.InsertBoardAsync("Viewer Board", adminId);
        var viewerId = await _factory.InsertStandardUserAsync("viewer@test.local");
        await _factory.InsertBoardMemberAsync(boardId, viewerId, "Viewer");
        var client = _factory.CreateAuthenticatedClient(
            TestPrincipals.RegisteredStandardUser(viewerId, "viewer@test.local"));

        var response = await client.PostAsJsonAsync($"/api/v1/boards/{boardId}/lanes", new { name = "Forbidden" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PostLane_DuplicateName_Returns409()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var boardId = await _factory.InsertBoardAsync("Dup Lane Board", adminId);
        await _factory.InsertLaneAsync(boardId, "Existing", 1);
        var client = _factory.CreateAuthenticatedClient(TestPrincipals.RegisteredAdmin(adminId));

        var response = await client.PostAsJsonAsync($"/api/v1/boards/{boardId}/lanes", new { name = "Existing" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("lane.name_conflict");
    }

    [Fact]
    public async Task PostLane_MissingName_Returns422()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var boardId = await _factory.InsertBoardAsync("Val Board", adminId);
        var client = _factory.CreateAuthenticatedClient(TestPrincipals.RegisteredAdmin(adminId));

        var response = await client.PostAsJsonAsync($"/api/v1/boards/{boardId}/lanes", new { name = "" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ── PATCH /api/v1/boards/{boardId}/lanes/{laneId} ────────────────────────

    [Fact]
    public async Task PatchLane_OwnerRenamesLane_Returns200WithUpdatedName()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var boardId = await _factory.InsertBoardAsync("Rename Board", adminId);
        var laneId = await _factory.InsertLaneAsync(boardId, "Old Name", 1);
        var client = _factory.CreateAuthenticatedClient(TestPrincipals.RegisteredAdmin(adminId));

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/boards/{boardId}/lanes/{laneId}",
            new { name = "New Name" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LaneDto>();
        body!.Name.Should().Be("New Name");
    }

    [Fact]
    public async Task PatchLane_DuplicateName_Returns409()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var boardId = await _factory.InsertBoardAsync("Dup Rename Board", adminId);
        await _factory.InsertLaneAsync(boardId, "Taken", 1);
        var laneId = await _factory.InsertLaneAsync(boardId, "Original", 2);
        var client = _factory.CreateAuthenticatedClient(TestPrincipals.RegisteredAdmin(adminId));

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/boards/{boardId}/lanes/{laneId}",
            new { name = "Taken" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ── DELETE /api/v1/boards/{boardId}/lanes/{laneId} ────────────────────────

    [Fact]
    public async Task DeleteLane_OwnerCaller_Returns204()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var boardId = await _factory.InsertBoardAsync("Del Lane Board", adminId);
        var laneId = await _factory.InsertLaneAsync(boardId, "Delete Me", 1);
        var client = _factory.CreateAuthenticatedClient(TestPrincipals.RegisteredAdmin(adminId));

        var response = await client.DeleteAsync($"/api/v1/boards/{boardId}/lanes/{laneId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteLane_ViewerCaller_Returns403()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var boardId = await _factory.InsertBoardAsync("View Del Board", adminId);
        var laneId = await _factory.InsertLaneAsync(boardId, "Protected", 1);
        var viewerId = await _factory.InsertStandardUserAsync("viewer2@test.local");
        await _factory.InsertBoardMemberAsync(boardId, viewerId, "Viewer");
        var client = _factory.CreateAuthenticatedClient(
            TestPrincipals.RegisteredStandardUser(viewerId, "viewer2@test.local"));

        var response = await client.DeleteAsync($"/api/v1/boards/{boardId}/lanes/{laneId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── POST /api/v1/boards/{boardId}/lanes/{laneId}/move ────────────────────

    [Fact]
    public async Task MoveLane_OwnerWithValidVersion_Returns200()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var boardId = await _factory.InsertBoardAsync("Move Board", adminId);
        var lane1Id = await _factory.InsertLaneAsync(boardId, "Lane A", 1);
        await _factory.InsertLaneAsync(boardId, "Lane B", 2);
        await _factory.InsertLaneAsync(boardId, "Lane C", 3);
        var client = _factory.CreateAuthenticatedClient(TestPrincipals.RegisteredAdmin(adminId));

        var response = await client.PostAsJsonAsync(
            $"/api/v1/boards/{boardId}/lanes/{lane1Id}/move",
            new { targetPosition = 3, expectedVersion = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MoveLane_VersionMismatch_Returns409()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var boardId = await _factory.InsertBoardAsync("Move Conflict Board", adminId);
        var laneId = await _factory.InsertLaneAsync(boardId, "Contested", 1);
        await _factory.InsertLaneAsync(boardId, "Other", 2);
        var client = _factory.CreateAuthenticatedClient(TestPrincipals.RegisteredAdmin(adminId));

        var response = await client.PostAsJsonAsync(
            $"/api/v1/boards/{boardId}/lanes/{laneId}/move",
            new { newPosition = 2, expectedVersion = 99 });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("lane.version_conflict");
    }

    [Fact]
    public async Task MoveLane_ViewerCaller_Returns403()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var boardId = await _factory.InsertBoardAsync("View Move Board", adminId);
        var laneId = await _factory.InsertLaneAsync(boardId, "Read Only", 1);
        await _factory.InsertLaneAsync(boardId, "Second", 2);
        var viewerId = await _factory.InsertStandardUserAsync("viewer3@test.local");
        await _factory.InsertBoardMemberAsync(boardId, viewerId, "Viewer");
        var client = _factory.CreateAuthenticatedClient(
            TestPrincipals.RegisteredStandardUser(viewerId, "viewer3@test.local"));

        var response = await client.PostAsJsonAsync(
            $"/api/v1/boards/{boardId}/lanes/{laneId}/move",
            new { newPosition = 2, expectedVersion = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
