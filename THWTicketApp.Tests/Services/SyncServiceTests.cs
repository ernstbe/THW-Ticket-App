using System.Text.Json;
using FluentAssertions;
using NSubstitute;
using THWTicketApp.Data;
using THWTicketApp.Models;
using THWTicketApp.Services;
using Xunit;

namespace THWTicketApp.Tests.Services;

public class SyncServiceTests : IDisposable
{
    private readonly IDatabaseService _db;
    private readonly ITrueDeskApiService _api;
    private readonly FakeConnectivity _connectivity;
    private readonly SyncService _sut;

    public SyncServiceTests()
    {
        _db = Substitute.For<IDatabaseService>();
        _api = Substitute.For<ITrueDeskApiService>();

        _connectivity = new FakeConnectivity { NetworkAccess = NetworkAccess.Internet };
        Connectivity.SetCurrent(_connectivity);

        // Default: authenticated, no pending actions
        _api.IsAuthenticated.Returns(true);
        _db.GetPendingActionsAsync().Returns(new List<PendingAction>());
        _db.GetPendingActionCountAsync().Returns(0);

        _sut = new SyncService(_db, _api);
    }

    public void Dispose()
    {
        // Reset static state so tests don't leak into each other
        Connectivity.SetCurrent(new FakeConnectivity { NetworkAccess = NetworkAccess.Internet });
    }

    // ──────────────────────────────────────────────
    // Enqueue tests
    // ──────────────────────────────────────────────

    [Fact]
    public async Task EnqueueCommentAsync_SerializesCorrectPayloadAndActionType()
    {
        var updatedAt = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        string? capturedPayload = null;
        _db.EnqueueActionAsync(Arg.Any<string>(), Arg.Do<string>(s => capturedPayload = s), Arg.Any<DateTime?>())
            .Returns(Task.CompletedTask);

        await _sut.EnqueueCommentAsync("ticket-1", 100, "owner-1", "Hello!", updatedAt);

        await _db.Received(1).EnqueueActionAsync("AddComment", Arg.Any<string>(), updatedAt);
        capturedPayload.Should().NotBeNull();
        var doc = JsonDocument.Parse(capturedPayload!);
        doc.RootElement.GetProperty("ticketId").GetString().Should().Be("ticket-1");
        doc.RootElement.GetProperty("ticketUid").GetInt32().Should().Be(100);
        doc.RootElement.GetProperty("ownerId").GetString().Should().Be("owner-1");
        doc.RootElement.GetProperty("comment").GetString().Should().Be("Hello!");
    }

    [Fact]
    public async Task EnqueueCommentAsync_WithoutUpdatedAt_PassesNull()
    {
        await _sut.EnqueueCommentAsync("t1", 1, "o1", "text");

        await _db.Received(1).EnqueueActionAsync(
            "AddComment",
            Arg.Any<string>(),
            null);
    }

    [Fact]
    public async Task EnqueueNoteAsync_SerializesCorrectPayloadAndActionType()
    {
        var updatedAt = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        string? capturedPayload = null;
        _db.EnqueueActionAsync(Arg.Any<string>(), Arg.Do<string>(s => capturedPayload = s), Arg.Any<DateTime?>())
            .Returns(Task.CompletedTask);

        await _sut.EnqueueNoteAsync("ticket-2", 200, "owner-2", "A note", updatedAt);

        await _db.Received(1).EnqueueActionAsync("AddNote", Arg.Any<string>(), updatedAt);
        capturedPayload.Should().NotBeNull();
        var doc = JsonDocument.Parse(capturedPayload!);
        doc.RootElement.GetProperty("ticketId").GetString().Should().Be("ticket-2");
        doc.RootElement.GetProperty("ticketUid").GetInt32().Should().Be(200);
        doc.RootElement.GetProperty("ownerId").GetString().Should().Be("owner-2");
        doc.RootElement.GetProperty("note").GetString().Should().Be("A note");
    }

    [Fact]
    public async Task EnqueueCreateTicketAsync_SerializesAllFields()
    {
        string? capturedPayload = null;
        _db.EnqueueActionAsync(Arg.Any<string>(), Arg.Do<string>(s => capturedPayload = s), Arg.Any<DateTime?>())
            .Returns(Task.CompletedTask);

        await _sut.EnqueueCreateTicketAsync("Subj", "Issue text", "type1", "prio1", "grp1", "user1");

        await _db.Received(1).EnqueueActionAsync("CreateTicket", Arg.Any<string>(), Arg.Any<DateTime?>());
        capturedPayload.Should().NotBeNull();
        var doc = JsonDocument.Parse(capturedPayload!);
        doc.RootElement.GetProperty("subject").GetString().Should().Be("Subj");
        doc.RootElement.GetProperty("issue").GetString().Should().Be("Issue text");
        doc.RootElement.GetProperty("typeId").GetString().Should().Be("type1");
        doc.RootElement.GetProperty("priorityId").GetString().Should().Be("prio1");
        doc.RootElement.GetProperty("groupId").GetString().Should().Be("grp1");
        doc.RootElement.GetProperty("assigneeId").GetString().Should().Be("user1");
    }

    [Fact]
    public async Task EnqueueCreateTicketAsync_NullableFieldsSerializedAsNull()
    {
        string? capturedPayload = null;
        _db.EnqueueActionAsync(Arg.Any<string>(), Arg.Do<string>(s => capturedPayload = s), Arg.Any<DateTime?>())
            .Returns(Task.CompletedTask);

        await _sut.EnqueueCreateTicketAsync("Subj", null, null, null, null, null);

        capturedPayload.Should().NotBeNull();
        var doc = JsonDocument.Parse(capturedPayload!);
        doc.RootElement.GetProperty("subject").GetString().Should().Be("Subj");
        doc.RootElement.GetProperty("issue").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task EnqueueAssignAsync_SerializesCorrectPayload()
    {
        var updatedAt = new DateTime(2025, 7, 1, 12, 0, 0, DateTimeKind.Utc);
        string? capturedPayload = null;
        _db.EnqueueActionAsync(Arg.Any<string>(), Arg.Do<string>(s => capturedPayload = s), Arg.Any<DateTime?>())
            .Returns(Task.CompletedTask);

        await _sut.EnqueueAssignAsync("ticket-3", 300, "user-5", updatedAt);

        await _db.Received(1).EnqueueActionAsync("AssignTicket", Arg.Any<string>(), updatedAt);
        capturedPayload.Should().NotBeNull();
        var doc = JsonDocument.Parse(capturedPayload!);
        doc.RootElement.GetProperty("ticketId").GetString().Should().Be("ticket-3");
        doc.RootElement.GetProperty("ticketUid").GetInt32().Should().Be(300);
        doc.RootElement.GetProperty("userId").GetString().Should().Be("user-5");
    }

    [Fact]
    public async Task EnqueueCommentAsync_NotifiesPendingCountChanged()
    {
        _db.GetPendingActionCountAsync().Returns(3);
        int? notifiedCount = null;
        _sut.PendingCountChanged += count => notifiedCount = count;

        await _sut.EnqueueCommentAsync("t1", 1, "o1", "text");

        notifiedCount.Should().Be(3);
    }

    [Fact]
    public async Task EnqueueNoteAsync_NotifiesPendingCountChanged()
    {
        _db.GetPendingActionCountAsync().Returns(1);
        int? notifiedCount = null;
        _sut.PendingCountChanged += count => notifiedCount = count;

        await _sut.EnqueueNoteAsync("t1", 1, "o1", "note text");

        notifiedCount.Should().Be(1);
    }

    [Fact]
    public async Task EnqueueCreateTicketAsync_NotifiesPendingCountChanged()
    {
        _db.GetPendingActionCountAsync().Returns(5);
        int? notifiedCount = null;
        _sut.PendingCountChanged += count => notifiedCount = count;

        await _sut.EnqueueCreateTicketAsync("subj", null, null, null, null, null);

        notifiedCount.Should().Be(5);
    }

    [Fact]
    public async Task EnqueueAssignAsync_NotifiesPendingCountChanged()
    {
        _db.GetPendingActionCountAsync().Returns(2);
        int? notifiedCount = null;
        _sut.PendingCountChanged += count => notifiedCount = count;

        await _sut.EnqueueAssignAsync("t1", 1, "u1");

        notifiedCount.Should().Be(2);
    }

    // ──────────────────────────────────────────────
    // GetPendingCountAsync
    // ──────────────────────────────────────────────

    [Fact]
    public async Task GetPendingCountAsync_DelegatesToDatabaseService()
    {
        _db.GetPendingActionCountAsync().Returns(42);

        var result = await _sut.GetPendingCountAsync();

        result.Should().Be(42);
    }

    // ──────────────────────────────────────────────
    // GetConflictedActionsAsync
    // ──────────────────────────────────────────────

    [Fact]
    public async Task GetConflictedActionsAsync_ReturnsOnlyConflictedActions()
    {
        var actions = new List<PendingAction>
        {
            new() { Id = 1, IsConflicted = false },
            new() { Id = 2, IsConflicted = true, ConflictReason = "conflict!" },
            new() { Id = 3, IsConflicted = false },
            new() { Id = 4, IsConflicted = true, ConflictReason = "another conflict" },
        };
        _db.GetPendingActionsAsync().Returns(actions);

        var result = await _sut.GetConflictedActionsAsync();

        result.Should().HaveCount(2);
        result.Select(a => a.Id).Should().BeEquivalentTo(new[] { 2, 4 });
    }

    [Fact]
    public async Task GetConflictedActionsAsync_ReturnsEmptyWhenNoConflicts()
    {
        var actions = new List<PendingAction>
        {
            new() { Id = 1, IsConflicted = false },
        };
        _db.GetPendingActionsAsync().Returns(actions);

        var result = await _sut.GetConflictedActionsAsync();

        result.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────
    // DiscardActionAsync
    // ──────────────────────────────────────────────

    [Fact]
    public async Task DiscardActionAsync_RemovesActionAndNotifies()
    {
        _db.GetPendingActionCountAsync().Returns(0);
        int? notifiedCount = null;
        _sut.PendingCountChanged += count => notifiedCount = count;

        await _sut.DiscardActionAsync(7);

        await _db.Received(1).RemoveActionAsync(7);
        notifiedCount.Should().Be(0);
    }

    // ──────────────────────────────────────────────
    // ForceApplyAsync
    // ──────────────────────────────────────────────

    [Fact]
    public async Task ForceApplyAsync_ProcessesActionAndRemovesOnSuccess()
    {
        var action = new PendingAction
        {
            Id = 10,
            ActionType = "AddComment",
            PayloadJson = JsonSerializer.Serialize(new { ticketId = "t1", ownerId = "o1", comment = "hi" }),
            IsConflicted = true,
        };
        _db.GetPendingActionsAsync().Returns(new List<PendingAction> { action });
        _api.AddCommentAsync("t1", "o1", "hi").Returns(true);
        _db.GetPendingActionCountAsync().Returns(0);

        var result = await _sut.ForceApplyAsync(10);

        result.Should().BeTrue();
        await _db.Received(1).RemoveActionAsync(10);
    }

    [Fact]
    public async Task ForceApplyAsync_ReturnsFalseWhenActionNotFound()
    {
        _db.GetPendingActionsAsync().Returns(new List<PendingAction>());

        var result = await _sut.ForceApplyAsync(999);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ForceApplyAsync_ReturnsFalseAndDoesNotRemoveOnApiFailure()
    {
        var action = new PendingAction
        {
            Id = 11,
            ActionType = "AddComment",
            PayloadJson = JsonSerializer.Serialize(new { ticketId = "t1", ownerId = "o1", comment = "hi" }),
        };
        _db.GetPendingActionsAsync().Returns(new List<PendingAction> { action });
        _api.AddCommentAsync("t1", "o1", "hi").Returns(false);

        var result = await _sut.ForceApplyAsync(11);

        result.Should().BeFalse();
        await _db.DidNotReceive().RemoveActionAsync(11);
    }

    // ──────────────────────────────────────────────
    // SyncPendingActionsAsync – guard conditions
    // ──────────────────────────────────────────────

    [Fact]
    public async Task SyncPendingActionsAsync_ReturnsFalseWhenNotAuthenticated()
    {
        _api.IsAuthenticated.Returns(false);

        var result = await _sut.SyncPendingActionsAsync();

        result.Should().BeFalse();
        await _db.DidNotReceive().GetPendingActionsAsync();
    }

    [Fact]
    public async Task SyncPendingActionsAsync_ReturnsFalseWhenNoNetwork()
    {
        _connectivity.NetworkAccess = NetworkAccess.None;

        var result = await _sut.SyncPendingActionsAsync();

        result.Should().BeFalse();
        await _db.DidNotReceive().GetPendingActionsAsync();
    }

    [Fact]
    public async Task SyncPendingActionsAsync_ReturnsTrueWhenNoPendingActions()
    {
        _db.GetPendingActionsAsync().Returns(new List<PendingAction>());

        var result = await _sut.SyncPendingActionsAsync();

        result.Should().BeTrue();
    }

    // ──────────────────────────────────────────────
    // SyncPendingActionsAsync – conflicted actions
    // ──────────────────────────────────────────────

    [Fact]
    public async Task SyncPendingActionsAsync_SkipsAlreadyConflictedActions()
    {
        var action = new PendingAction
        {
            Id = 1,
            ActionType = "AddComment",
            PayloadJson = JsonSerializer.Serialize(new { ticketId = "t1", ownerId = "o1", comment = "text" }),
            IsConflicted = true,
            ConflictReason = "already conflicted",
        };
        _db.GetPendingActionsAsync().Returns(new List<PendingAction> { action });

        var result = await _sut.SyncPendingActionsAsync();

        result.Should().BeFalse(); // not all succeeded because conflicted action was skipped
        // Should NOT try to process the action
        await _api.DidNotReceive().AddCommentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    // ──────────────────────────────────────────────
    // SyncPendingActionsAsync – successful processing
    // ──────────────────────────────────────────────

    [Fact]
    public async Task SyncPendingActionsAsync_ProcessesAddCommentAndRemovesAction()
    {
        var action = new PendingAction
        {
            Id = 1,
            ActionType = "AddComment",
            PayloadJson = JsonSerializer.Serialize(new { ticketId = "t1", ownerId = "o1", comment = "hello" }),
        };
        _db.GetPendingActionsAsync().Returns(new List<PendingAction> { action });
        _api.AddCommentAsync("t1", "o1", "hello").Returns(true);

        var result = await _sut.SyncPendingActionsAsync();

        result.Should().BeTrue();
        await _db.Received(1).RemoveActionAsync(1);
    }

    [Fact]
    public async Task SyncPendingActionsAsync_ProcessesAddNoteAndRemovesAction()
    {
        var action = new PendingAction
        {
            Id = 2,
            ActionType = "AddNote",
            PayloadJson = JsonSerializer.Serialize(new { ticketId = "t2", ownerId = "o2", note = "a note" }),
        };
        _db.GetPendingActionsAsync().Returns(new List<PendingAction> { action });
        _api.AddNoteAsync("t2", "o2", "a note").Returns(true);

        var result = await _sut.SyncPendingActionsAsync();

        result.Should().BeTrue();
        await _db.Received(1).RemoveActionAsync(2);
    }

    [Fact]
    public async Task SyncPendingActionsAsync_ProcessesCreateTicketAndRemovesAction()
    {
        var action = new PendingAction
        {
            Id = 3,
            ActionType = "CreateTicket",
            PayloadJson = JsonSerializer.Serialize(new
            {
                subject = "New ticket",
                issue = "Some issue",
                typeId = "type1",
                priorityId = "prio1",
                groupId = "grp1",
                assigneeId = "user1",
            }),
        };
        _db.GetPendingActionsAsync().Returns(new List<PendingAction> { action });
        _api.CreateTicketAsync("New ticket", "Some issue", "type1", "prio1", "grp1", "user1").Returns(true);

        var result = await _sut.SyncPendingActionsAsync();

        result.Should().BeTrue();
        await _db.Received(1).RemoveActionAsync(3);
    }

    [Fact]
    public async Task SyncPendingActionsAsync_ProcessesAssignTicketAndRemovesAction()
    {
        var action = new PendingAction
        {
            Id = 4,
            ActionType = "AssignTicket",
            PayloadJson = JsonSerializer.Serialize(new { ticketId = "t4", userId = "u4" }),
        };
        _db.GetPendingActionsAsync().Returns(new List<PendingAction> { action });
        _api.AssignTicketAsync("t4", "u4").Returns(true);

        var result = await _sut.SyncPendingActionsAsync();

        result.Should().BeTrue();
        await _db.Received(1).RemoveActionAsync(4);
    }

    // ──────────────────────────────────────────────
    // SyncPendingActionsAsync – retry / failure
    // ──────────────────────────────────────────────

    [Fact]
    public async Task SyncPendingActionsAsync_IncrementsRetryCountOnFailure()
    {
        var action = new PendingAction
        {
            Id = 5,
            ActionType = "AddComment",
            PayloadJson = JsonSerializer.Serialize(new { ticketId = "t1", ownerId = "o1", comment = "x" }),
            RetryCount = 0,
        };
        _db.GetPendingActionsAsync().Returns(new List<PendingAction> { action });
        _api.AddCommentAsync("t1", "o1", "x").Returns(false);

        var result = await _sut.SyncPendingActionsAsync();

        result.Should().BeFalse();
        await _db.Received(1).IncrementRetryCountAsync(5);
        await _db.DidNotReceive().RemoveActionAsync(5);
    }

    [Fact]
    public async Task SyncPendingActionsAsync_RemovesActionAfterMaxRetries()
    {
        var action = new PendingAction
        {
            Id = 6,
            ActionType = "AddComment",
            PayloadJson = JsonSerializer.Serialize(new { ticketId = "t1", ownerId = "o1", comment = "x" }),
            RetryCount = 5, // >= 5 triggers removal
        };
        _db.GetPendingActionsAsync().Returns(new List<PendingAction> { action });
        _api.AddCommentAsync("t1", "o1", "x").Returns(false);

        var result = await _sut.SyncPendingActionsAsync();

        result.Should().BeFalse();
        await _db.Received(1).IncrementRetryCountAsync(6);
        await _db.Received(1).RemoveActionAsync(6);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(10)]
    public async Task SyncPendingActionsAsync_RemovesActionWhenRetryCountIsAtOrAboveThreshold(int retryCount)
    {
        var action = new PendingAction
        {
            Id = 7,
            ActionType = "AddNote",
            PayloadJson = JsonSerializer.Serialize(new { ticketId = "t1", ownerId = "o1", note = "n" }),
            RetryCount = retryCount,
        };
        _db.GetPendingActionsAsync().Returns(new List<PendingAction> { action });
        _api.AddNoteAsync("t1", "o1", "n").Returns(false);

        await _sut.SyncPendingActionsAsync();

        await _db.Received(1).RemoveActionAsync(7);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(4)]
    public async Task SyncPendingActionsAsync_DoesNotRemoveActionBelowRetryThreshold(int retryCount)
    {
        var action = new PendingAction
        {
            Id = 8,
            ActionType = "AddNote",
            PayloadJson = JsonSerializer.Serialize(new { ticketId = "t1", ownerId = "o1", note = "n" }),
            RetryCount = retryCount,
        };
        _db.GetPendingActionsAsync().Returns(new List<PendingAction> { action });
        _api.AddNoteAsync("t1", "o1", "n").Returns(false);

        await _sut.SyncPendingActionsAsync();

        await _db.DidNotReceive().RemoveActionAsync(8);
    }

    [Fact]
    public async Task SyncPendingActionsAsync_UnknownActionType_ReturnsFalse()
    {
        var action = new PendingAction
        {
            Id = 9,
            ActionType = "UnknownAction",
            PayloadJson = "{}",
            RetryCount = 0,
        };
        _db.GetPendingActionsAsync().Returns(new List<PendingAction> { action });

        var result = await _sut.SyncPendingActionsAsync();

        result.Should().BeFalse();
        await _db.Received(1).IncrementRetryCountAsync(9);
    }

    // ──────────────────────────────────────────────
    // SyncPendingActionsAsync – conflict detection
    // ──────────────────────────────────────────────

    [Fact]
    public async Task SyncPendingActionsAsync_DetectsConflictWhenTicketUpdatedAfterQueuing()
    {
        var queuedAt = new DateTime(2025, 6, 1, 10, 0, 0, DateTimeKind.Utc);
        var action = new PendingAction
        {
            Id = 20,
            ActionType = "AddComment",
            PayloadJson = JsonSerializer.Serialize(new { ticketId = "t1", ticketUid = 100, ownerId = "o1", comment = "text" }),
            TicketUpdatedAt = queuedAt,
        };
        _db.GetPendingActionsAsync().Returns(new List<PendingAction> { action });

        // Ticket was updated well after the action was queued
        var serverTicket = new Ticket
        {
            Updated = queuedAt.AddHours(2),
            Assignee = new Assignee { Fullname = "Max Mustermann" },
        };
        var wrappedJson = JsonSerializer.Serialize(new { success = true, ticket = serverTicket });
        _api.GetTicketAsync("100").Returns(wrappedJson);

        PendingAction? detectedConflict = null;
        _sut.ConflictDetected += a => detectedConflict = a;

        var result = await _sut.SyncPendingActionsAsync();

        result.Should().BeFalse();
        await _db.Received(1).MarkActionConflictedAsync(20, Arg.Is<string>(s => s.Contains("Max Mustermann")));
        detectedConflict.Should().NotBeNull();
        detectedConflict!.Id.Should().Be(20);
        detectedConflict.IsConflicted.Should().BeTrue();
    }

    [Fact]
    public async Task SyncPendingActionsAsync_NoConflictWhenTicketNotUpdatedSinceQueuing()
    {
        var queuedAt = new DateTime(2025, 6, 1, 10, 0, 0, DateTimeKind.Utc);
        var action = new PendingAction
        {
            Id = 21,
            ActionType = "AddComment",
            PayloadJson = JsonSerializer.Serialize(new { ticketId = "t1", ticketUid = 101, ownerId = "o1", comment = "text" }),
            TicketUpdatedAt = queuedAt,
        };
        _db.GetPendingActionsAsync().Returns(new List<PendingAction> { action });

        // Ticket Updated is same as queued time (within 1 second tolerance)
        var serverTicket = new Ticket { Updated = queuedAt };
        var wrappedJson = JsonSerializer.Serialize(new { success = true, ticket = serverTicket });
        _api.GetTicketAsync("101").Returns(wrappedJson);
        _api.AddCommentAsync("t1", "o1", "text").Returns(true);

        var result = await _sut.SyncPendingActionsAsync();

        result.Should().BeTrue();
        await _db.DidNotReceive().MarkActionConflictedAsync(Arg.Any<int>(), Arg.Any<string>());
        await _db.Received(1).RemoveActionAsync(21);
    }

    [Fact]
    public async Task SyncPendingActionsAsync_CreateTicketSkipsConflictCheck()
    {
        var action = new PendingAction
        {
            Id = 22,
            ActionType = "CreateTicket",
            PayloadJson = JsonSerializer.Serialize(new
            {
                subject = "New",
                issue = (string?)null,
                typeId = (string?)null,
                priorityId = (string?)null,
                groupId = (string?)null,
                assigneeId = (string?)null,
            }),
            TicketUpdatedAt = DateTime.UtcNow, // Even with TicketUpdatedAt set
        };
        _db.GetPendingActionsAsync().Returns(new List<PendingAction> { action });
        _api.CreateTicketAsync("New", null, null, null, null, null).Returns(true);

        var result = await _sut.SyncPendingActionsAsync();

        result.Should().BeTrue();
        // Should NOT call GetTicketAsync for conflict checking
        await _api.DidNotReceive().GetTicketAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task SyncPendingActionsAsync_NoConflictCheckWhenTicketUpdatedAtIsNull()
    {
        var action = new PendingAction
        {
            Id = 23,
            ActionType = "AddComment",
            PayloadJson = JsonSerializer.Serialize(new { ticketId = "t1", ownerId = "o1", comment = "text" }),
            TicketUpdatedAt = null, // No timestamp to compare
        };
        _db.GetPendingActionsAsync().Returns(new List<PendingAction> { action });
        _api.AddCommentAsync("t1", "o1", "text").Returns(true);

        var result = await _sut.SyncPendingActionsAsync();

        result.Should().BeTrue();
        await _api.DidNotReceive().GetTicketAsync(Arg.Any<string>());
    }

    // ──────────────────────────────────────────────
    // SyncPendingActionsAsync – multiple actions
    // ──────────────────────────────────────────────

    [Fact]
    public async Task SyncPendingActionsAsync_ProcessesMultipleActionsInOrder()
    {
        var action1 = new PendingAction
        {
            Id = 30,
            ActionType = "AddComment",
            PayloadJson = JsonSerializer.Serialize(new { ticketId = "t1", ownerId = "o1", comment = "first" }),
        };
        var action2 = new PendingAction
        {
            Id = 31,
            ActionType = "AddNote",
            PayloadJson = JsonSerializer.Serialize(new { ticketId = "t2", ownerId = "o2", note = "second" }),
        };
        _db.GetPendingActionsAsync().Returns(new List<PendingAction> { action1, action2 });
        _api.AddCommentAsync("t1", "o1", "first").Returns(true);
        _api.AddNoteAsync("t2", "o2", "second").Returns(true);

        var result = await _sut.SyncPendingActionsAsync();

        result.Should().BeTrue();
        await _db.Received(1).RemoveActionAsync(30);
        await _db.Received(1).RemoveActionAsync(31);
    }

    [Fact]
    public async Task SyncPendingActionsAsync_PartialFailureReturnsFalse()
    {
        var action1 = new PendingAction
        {
            Id = 40,
            ActionType = "AddComment",
            PayloadJson = JsonSerializer.Serialize(new { ticketId = "t1", ownerId = "o1", comment = "ok" }),
        };
        var action2 = new PendingAction
        {
            Id = 41,
            ActionType = "AddComment",
            PayloadJson = JsonSerializer.Serialize(new { ticketId = "t2", ownerId = "o2", comment = "fail" }),
            RetryCount = 0,
        };
        _db.GetPendingActionsAsync().Returns(new List<PendingAction> { action1, action2 });
        _api.AddCommentAsync("t1", "o1", "ok").Returns(true);
        _api.AddCommentAsync("t2", "o2", "fail").Returns(false);

        var result = await _sut.SyncPendingActionsAsync();

        result.Should().BeFalse();
        await _db.Received(1).RemoveActionAsync(40);
        await _db.DidNotReceive().RemoveActionAsync(41);
    }

    // ──────────────────────────────────────────────
    // SyncPendingActionsAsync – reentrancy guard
    // ──────────────────────────────────────────────

    [Fact]
    public async Task SyncPendingActionsAsync_NotifiesPendingCountAfterSync()
    {
        _db.GetPendingActionsAsync().Returns(new List<PendingAction>());
        _db.GetPendingActionCountAsync().Returns(0);

        int? notifiedCount = null;
        _sut.PendingCountChanged += count => notifiedCount = count;

        await _sut.SyncPendingActionsAsync();

        notifiedCount.Should().Be(0);
    }

    // ──────────────────────────────────────────────
    // ProcessActionAsync – malformed payload
    // ──────────────────────────────────────────────

    [Fact]
    public async Task SyncPendingActionsAsync_MalformedPayloadTreatedAsFailure()
    {
        var action = new PendingAction
        {
            Id = 50,
            ActionType = "AddComment",
            PayloadJson = "not valid json",
            RetryCount = 0,
        };
        _db.GetPendingActionsAsync().Returns(new List<PendingAction> { action });

        var result = await _sut.SyncPendingActionsAsync();

        result.Should().BeFalse();
        await _db.Received(1).IncrementRetryCountAsync(50);
    }

    // ──────────────────────────────────────────────
    // Connectivity change event
    // ──────────────────────────────────────────────

    [Fact]
    public async Task OnConnectivityChanged_TriggersSync_WhenInternetRestored()
    {
        // Set up a pending action that will succeed
        var action = new PendingAction
        {
            Id = 60,
            ActionType = "AddComment",
            PayloadJson = JsonSerializer.Serialize(new { ticketId = "t1", ownerId = "o1", comment = "test" }),
        };
        _db.GetPendingActionsAsync().Returns(new List<PendingAction> { action });
        _api.AddCommentAsync("t1", "o1", "test").Returns(true);

        // Raise the connectivity changed event with Internet access
        Connectivity.RaiseConnectivityChanged(new ConnectivityChangedEventArgs
        {
            NetworkAccess = NetworkAccess.Internet,
        });

        // Give the async void handler time to complete
        await Task.Delay(200);

        await _db.Received().GetPendingActionsAsync();
    }

    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────

    /// <summary>Fake IConnectivity for controlling network state in tests.</summary>
    private class FakeConnectivity : IConnectivity
    {
        public NetworkAccess NetworkAccess { get; set; } = NetworkAccess.Internet;

        public event EventHandler<ConnectivityChangedEventArgs>? ConnectivityChanged
        {
            add { }
            remove { }
        }
    }
}
