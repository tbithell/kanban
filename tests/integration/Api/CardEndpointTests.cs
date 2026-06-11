using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Kanban.Contracts;
using Kanban.Tests.Integration.Infrastructure;
using Xunit;

namespace Kanban.Tests.Integration.Api;

public sealed class CardEndpointTests : IClassFixture<KanbanWebAppFactory>
{
    private readonly KanbanWebAppFactory _factory;

    public CardEndpointTests(KanbanWebAppFactory factory)
    {
        _factory = factory;
    }

    // ── POST /api/v1/boards/{boardId}/lanes/{laneId}/cards ───────────────────

    [Fact]
    public async Task PostCard_OwnerWithTitle_Returns201WithCardDto()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var boardId = await _factory.InsertBoardAsync("Card Create Board", adminId);
        var laneId = await _factory.InsertLaneAsync(boardId, "To Do", 1);
        var client = _factory.CreateAuthenticatedClient(TestPrincipals.RegisteredAdmin(adminId));

        var response = await client.PostAsJsonAsync(
            $"/api/v1/boards/{boardId}/lanes/{laneId}/cards",
            new { title = "My First Card" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<CardDto>();
        body.Should().NotBeNull();
        body!.Title.Should().Be("My First Card");
        body.Position.Should().Be(1);
        body.LaneId.Should().Be(laneId);
        body.BoardId.Should().Be(boardId);
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task PostCard_SecondCard_AppendsAtPositionTwo()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var boardId = await _factory.InsertBoardAsync("Two Card Board", adminId);
        var laneId = await _factory.InsertLaneAsync(boardId, "Lane", 1);
        await _factory.InsertCardAsync(laneId, boardId, "Existing Card", 1);
        var client = _factory.CreateAuthenticatedClient(TestPrincipals.RegisteredAdmin(adminId));

        var response = await client.PostAsJsonAsync(
            $"/api/v1/boards/{boardId}/lanes/{laneId}/cards",
            new { title = "Second Card" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<CardDto>();
        body!.Position.Should().Be(2);
    }

    [Fact]
    public async Task PostCard_ViewerCaller_Returns403()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var boardId = await _factory.InsertBoardAsync("Card Viewer Board", adminId);
        var laneId = await _factory.InsertLaneAsync(boardId, "Lane", 1);
        var viewerId = await _factory.InsertStandardUserAsync("card-viewer@test.local");
        await _factory.InsertBoardMemberAsync(boardId, viewerId, "Viewer");
        var client = _factory.CreateAuthenticatedClient(
            TestPrincipals.RegisteredStandardUser(viewerId, "card-viewer@test.local"));

        var response = await client.PostAsJsonAsync(
            $"/api/v1/boards/{boardId}/lanes/{laneId}/cards",
            new { title = "Forbidden Card" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PostCard_EmptyTitle_Returns422()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var boardId = await _factory.InsertBoardAsync("Card Val Board", adminId);
        var laneId = await _factory.InsertLaneAsync(boardId, "Lane", 1);
        var client = _factory.CreateAuthenticatedClient(TestPrincipals.RegisteredAdmin(adminId));

        var response = await client.PostAsJsonAsync(
            $"/api/v1/boards/{boardId}/lanes/{laneId}/cards",
            new { title = "" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ── PATCH /api/v1/boards/{boardId}/cards/{cardId} ─────────────────────────

    [Fact]
    public async Task PatchCard_OwnerUpdatesTitle_Returns200WithUpdatedDto()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var boardId = await _factory.InsertBoardAsync("Card Update Board", adminId);
        var laneId = await _factory.InsertLaneAsync(boardId, "Lane", 1);
        var cardId = await _factory.InsertCardAsync(laneId, boardId, "Original Title", 1);
        var client = _factory.CreateAuthenticatedClient(TestPrincipals.RegisteredAdmin(adminId));

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/boards/{boardId}/cards/{cardId}",
            new { title = "Updated Title", clearDueDate = false });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CardDto>();
        body!.Title.Should().Be("Updated Title");
    }

    [Fact]
    public async Task PatchCard_ClearDueDate_Returns200WithNullDueDate()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var boardId = await _factory.InsertBoardAsync("Card ClearDate Board", adminId);
        var laneId = await _factory.InsertLaneAsync(boardId, "Lane", 1);
        var cardId = await _factory.InsertCardAsync(laneId, boardId, "Task With Due Date", 1,
            dueDate: DateOnly.FromDateTime(DateTime.Today.AddDays(7)));
        var client = _factory.CreateAuthenticatedClient(TestPrincipals.RegisteredAdmin(adminId));

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/boards/{boardId}/cards/{cardId}",
            new { clearDueDate = true });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CardDto>();
        body!.DueDate.Should().BeNull();
    }

    // ── DELETE /api/v1/boards/{boardId}/cards/{cardId} ────────────────────────

    [Fact]
    public async Task DeleteCard_OwnerCaller_Returns204()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var boardId = await _factory.InsertBoardAsync("Card Delete Board", adminId);
        var laneId = await _factory.InsertLaneAsync(boardId, "Lane", 1);
        var cardId = await _factory.InsertCardAsync(laneId, boardId, "Delete Me", 1);
        var client = _factory.CreateAuthenticatedClient(TestPrincipals.RegisteredAdmin(adminId));

        var response = await client.DeleteAsync($"/api/v1/boards/{boardId}/cards/{cardId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteCard_RemainingPositionsGapless()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var boardId = await _factory.InsertBoardAsync("Card Gapless Board", adminId);
        var laneId = await _factory.InsertLaneAsync(boardId, "Lane", 1);
        var card1Id = await _factory.InsertCardAsync(laneId, boardId, "Card 1", 1);
        await _factory.InsertCardAsync(laneId, boardId, "Card 2", 2);
        await _factory.InsertCardAsync(laneId, boardId, "Card 3", 3);
        var client = _factory.CreateAuthenticatedClient(TestPrincipals.RegisteredAdmin(adminId));

        await client.DeleteAsync($"/api/v1/boards/{boardId}/cards/{card1Id}");

        var positions = await _factory.GetCardPositionsInLaneAsync(laneId);
        positions.Should().BeEquivalentTo(new[] { 1, 2 });
    }

    [Fact]
    public async Task DeleteCard_ViewerCaller_Returns403()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var boardId = await _factory.InsertBoardAsync("Card ViewDel Board", adminId);
        var laneId = await _factory.InsertLaneAsync(boardId, "Lane", 1);
        var cardId = await _factory.InsertCardAsync(laneId, boardId, "Protected Card", 1);
        var viewerId = await _factory.InsertStandardUserAsync("card-viewer2@test.local");
        await _factory.InsertBoardMemberAsync(boardId, viewerId, "Viewer");
        var client = _factory.CreateAuthenticatedClient(
            TestPrincipals.RegisteredStandardUser(viewerId, "card-viewer2@test.local"));

        var response = await client.DeleteAsync($"/api/v1/boards/{boardId}/cards/{cardId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── POST /api/v1/boards/{boardId}/cards/{cardId}/move ────────────────────

    [Fact]
    public async Task MoveCard_SameLane_Returns200WithUpdatedPosition()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var boardId = await _factory.InsertBoardAsync("Move Same Lane Board", adminId);
        var laneId = await _factory.InsertLaneAsync(boardId, "Lane", 1);
        var card1Id = await _factory.InsertCardAsync(laneId, boardId, "Card 1", 1);
        await _factory.InsertCardAsync(laneId, boardId, "Card 2", 2);
        await _factory.InsertCardAsync(laneId, boardId, "Card 3", 3);
        var client = _factory.CreateAuthenticatedClient(TestPrincipals.RegisteredAdmin(adminId));

        var response = await client.PostAsJsonAsync(
            $"/api/v1/boards/{boardId}/cards/{card1Id}/move",
            new { targetLaneId = laneId, targetPosition = 3, expectedVersion = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CardDto>();
        body!.Position.Should().Be(3);
        body.LaneId.Should().Be(laneId);
    }

    [Fact]
    public async Task MoveCard_CrossLane_Returns200WithUpdatedLaneId()
    {
        var adminId = await _factory.GetSeededAdminIdAsync();
        var boardId = await _factory.InsertBoardAsync("Move Cross Lane Board", adminId);
        var sourceLaneId = await _factory.InsertLaneAsync(boardId, "Source", 1);
        var destLaneId = await _factory.InsertLaneAsync(boardId, "Destination", 2);
        var cardId = await _factory.InsertCardAsync(sourceLaneId, boardId, "Moving Card", 1);
        var client = _factory.CreateAuthenticatedClient(TestPrincipals.RegisteredAdmin(adminId));

        var response = await client.PostAsJsonAsync(
            $"/api/v1/boards/{boardId}/cards/{cardId}/move",
            new { targetLaneId = destLaneId, targetPosition = 1, expectedVersion = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CardDto>();
        body!.LaneId.Should().Be(destLaneId);
        body.Position.Should().Be(1);
    }

    [Fact]
    public async Task MoveCard_ConcurrentVersionConflict_ExactlyOneSucceeds()
    {
        // Invariant: N concurrent moves with identical expectedVersion — exactly one wins;
        // the rest are rejected with card.version_conflict (409).
        var adminId = await _factory.GetSeededAdminIdAsync();
        var boardId = await _factory.InsertBoardAsync("Concurrent Move Board", adminId);
        var laneId = await _factory.InsertLaneAsync(boardId, "Lane", 1);
        var card1Id = await _factory.InsertCardAsync(laneId, boardId, "Contested Card", 1);
        await _factory.InsertCardAsync(laneId, boardId, "Card 2", 2);
        await _factory.InsertCardAsync(laneId, boardId, "Card 3", 3);
        var client = _factory.CreateAuthenticatedClient(TestPrincipals.RegisteredAdmin(adminId));

        var tasks = Enumerable.Range(0, 4).Select(_ =>
            client.PostAsJsonAsync(
                $"/api/v1/boards/{boardId}/cards/{card1Id}/move",
                new { targetLaneId = laneId, targetPosition = 3, expectedVersion = 1 }));
        var results = await Task.WhenAll(tasks);

        var statuses = results.Select(r => (int)r.StatusCode).ToArray();
        statuses.Count(s => s == 200).Should().Be(1);
        statuses.Count(s => s == 409).Should().Be(3);
        var conflictBody = await results
            .First(r => r.StatusCode == HttpStatusCode.Conflict)
            .Content.ReadAsStringAsync();
        conflictBody.Should().Contain("card.version_conflict");
    }
}
