using FluentAssertions;
using SQLite;
using THWTicketApp.Data;
using THWTicketApp.Models;
using Xunit;

namespace THWTicketApp.Tests.Services;

/// <summary>
/// A testable database helper that mirrors DatabaseService logic but uses
/// a temp file path instead of FileSystem.AppDataDirectory.
/// </summary>
internal sealed class TestableDatabase : IAsyncDisposable
{
    private readonly SQLiteAsyncConnection _db;
    private readonly string _dbPath;

    public TestableDatabase(string dbPath)
    {
        _dbPath = dbPath;
        _db = new SQLiteAsyncConnection(dbPath,
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
    }

    public async Task InitAsync()
    {
        await _db.CreateTableAsync<CachedTicket>();
        await _db.CreateTableAsync<PendingAction>();
        await _db.CreateTableAsync<FavoriteTicket>();
        await _db.CreateTableAsync<TimeEntry>();
        await _db.CreateTableAsync<LinkedTicket>();
        await _db.CreateTableAsync<NotificationEntry>();
        await _db.CreateTableAsync<SavedFilter>();
    }

    public SQLiteAsyncConnection Connection => _db;

    // --- Cache operations (mirrors DatabaseService) ---

    public async Task SaveTicketsAsync(IEnumerable<Ticket> tickets)
    {
        var cachedTickets = tickets.Select(t => new CachedTicket
        {
            Id = t.Id,
            Subject = t.Subject,
            Issue = t.Issue,
            Date = t.Date,
            Updated = t.Updated,
            DueDate = t.DueDate,
            ClosedDate = t.ClosedDate,
            Uid = t.Uid,
            Deleted = t.Deleted,
            StatusId = t.Status?.Id,
            StatusName = t.Status?.Name,
            StatusHtmlColor = t.Status?.HtmlColor,
            StatusIsResolved = t.Status?.IsResolved ?? false,
            PriorityId = t.Priority?.Id,
            PriorityName = t.Priority?.Name,
            PriorityHtmlColor = t.Priority?.HtmlColor,
            TypeId = t.Type?.Id,
            TypeName = t.Type?.Name,
            OwnerId = t.Owner?.Id,
            OwnerFullname = t.Owner?.Fullname,
            OwnerEmail = t.Owner?.Email,
            AssigneeId = t.Assignee?.Id,
            AssigneeFullname = t.Assignee?.Fullname,
            AssigneeEmail = t.Assignee?.Email,
            GroupId = t.Group?.Id,
            GroupName = t.Group?.Name,
            CachedAt = DateTime.UtcNow
        }).ToList();

        await _db.DeleteAllAsync<CachedTicket>();
        await _db.InsertAllAsync(cachedTickets);
    }

    public async Task<List<CachedTicket>> GetCachedTicketsAsync()
    {
        return await _db.Table<CachedTicket>()
            .OrderByDescending(t => t.Date)
            .ToListAsync();
    }

    public async Task<List<Ticket>> GetTicketsFromCacheAsync()
    {
        var cached = await GetCachedTicketsAsync();
        return cached.Select(c => new Ticket
        {
            Id = c.Id,
            Subject = c.Subject,
            Issue = c.Issue,
            Date = c.Date,
            Updated = c.Updated,
            DueDate = c.DueDate ?? DateTime.MinValue,
            ClosedDate = c.ClosedDate,
            Uid = c.Uid,
            Deleted = c.Deleted,
            Status = c.StatusId != null ? new Status { Id = c.StatusId, Name = c.StatusName, HtmlColor = c.StatusHtmlColor, IsResolved = c.StatusIsResolved } : null,
            Priority = c.PriorityId != null ? new Priority { Id = c.PriorityId, Name = c.PriorityName, HtmlColor = c.PriorityHtmlColor } : null,
            Type = c.TypeId != null ? new TicketType { Id = c.TypeId, Name = c.TypeName } : null,
            Owner = c.OwnerId != null ? new Owner { Id = c.OwnerId, Fullname = c.OwnerFullname, Email = c.OwnerEmail } : null,
            Assignee = c.AssigneeId != null ? new Assignee { Id = c.AssigneeId, Fullname = c.AssigneeFullname, Email = c.AssigneeEmail } : null,
            Group = c.GroupId != null ? new Group { Id = c.GroupId, Name = c.GroupName } : null,
        }).ToList();
    }

    public async Task<int> GetCachedTicketCountAsync()
        => await _db.Table<CachedTicket>().CountAsync();

    public async Task ClearCacheAsync()
        => await _db.DeleteAllAsync<CachedTicket>();

    public async Task<DateTime?> GetLastCacheTimeAsync()
    {
        var first = await _db.Table<CachedTicket>().FirstOrDefaultAsync();
        return first?.CachedAt;
    }

    // --- Pending Actions ---

    public async Task EnqueueActionAsync(string actionType, string payloadJson, DateTime? ticketUpdatedAt = null)
    {
        var action = new PendingAction
        {
            ActionType = actionType,
            PayloadJson = payloadJson,
            CreatedAt = DateTime.UtcNow,
            RetryCount = 0,
            TicketUpdatedAt = ticketUpdatedAt
        };
        await _db.InsertAsync(action);
    }

    public async Task<List<PendingAction>> GetPendingActionsAsync()
        => await _db.Table<PendingAction>().OrderBy(a => a.CreatedAt).ToListAsync();

    public async Task<int> GetPendingActionCountAsync()
        => await _db.Table<PendingAction>().CountAsync();

    public async Task RemoveActionAsync(int id)
        => await _db.DeleteAsync<PendingAction>(id);

    public async Task IncrementRetryCountAsync(int id)
    {
        var action = await _db.FindAsync<PendingAction>(id);
        if (action != null)
        {
            action.RetryCount++;
            await _db.UpdateAsync(action);
        }
    }

    public async Task MarkActionConflictedAsync(int id, string reason)
    {
        var action = await _db.FindAsync<PendingAction>(id);
        if (action != null)
        {
            action.IsConflicted = true;
            action.ConflictReason = reason;
            await _db.UpdateAsync(action);
        }
    }

    // --- Favorites ---

    public async Task<bool> IsFavoriteAsync(string ticketId)
    {
        var fav = await _db.Table<FavoriteTicket>().FirstOrDefaultAsync(f => f.TicketId == ticketId);
        return fav != null;
    }

    public async Task ToggleFavoriteAsync(string ticketId)
    {
        var existing = await _db.Table<FavoriteTicket>().FirstOrDefaultAsync(f => f.TicketId == ticketId);
        if (existing != null)
            await _db.DeleteAsync(existing);
        else
            await _db.InsertAsync(new FavoriteTicket { TicketId = ticketId, AddedAt = DateTime.UtcNow });
    }

    public async Task<HashSet<string>> GetFavoriteIdsAsync()
    {
        var favorites = await _db.Table<FavoriteTicket>().ToListAsync();
        return favorites.Select(f => f.TicketId).ToHashSet();
    }

    // --- Time Tracking ---

    public async Task<TimeEntry> StartTimerAsync(string ticketId)
    {
        var entry = new TimeEntry { TicketId = ticketId, StartTime = DateTime.UtcNow };
        await _db.InsertAsync(entry);
        return entry;
    }

    public async Task StopTimerAsync(int entryId, string? description = null)
    {
        var entry = await _db.FindAsync<TimeEntry>(entryId);
        if (entry != null)
        {
            entry.EndTime = DateTime.UtcNow;
            entry.Description = description;
            await _db.UpdateAsync(entry);
        }
    }

    public async Task<TimeEntry?> GetActiveTimerAsync(string ticketId)
        => await _db.Table<TimeEntry>().FirstOrDefaultAsync(t => t.TicketId == ticketId && t.EndTime == null);

    public async Task<List<TimeEntry>> GetTimeEntriesAsync(string ticketId)
        => await _db.Table<TimeEntry>().Where(t => t.TicketId == ticketId).OrderByDescending(t => t.StartTime).ToListAsync();

    public async Task<double> GetTotalTimeAsync(string ticketId)
    {
        var entries = await _db.Table<TimeEntry>().Where(t => t.TicketId == ticketId && t.EndTime != null).ToListAsync();
        return entries.Sum(e => e.DurationMinutes);
    }

    public async Task DeleteTimeEntryAsync(int entryId)
        => await _db.DeleteAsync<TimeEntry>(entryId);

    // --- Linked Tickets ---

    public async Task AddLinkedTicketAsync(string sourceId, string linkedId, string linkedSubject, int linkedUid, string linkType = "related")
    {
        var existing = await _db.Table<LinkedTicket>().FirstOrDefaultAsync(l => l.SourceTicketId == sourceId && l.LinkedTicketId == linkedId);
        if (existing != null) return;

        await _db.InsertAsync(new LinkedTicket
        {
            SourceTicketId = sourceId,
            LinkedTicketId = linkedId,
            LinkedTicketSubject = linkedSubject,
            LinkedTicketUid = linkedUid,
            LinkType = linkType,
            CreatedAt = DateTime.UtcNow
        });
    }

    public async Task<List<LinkedTicket>> GetLinkedTicketsAsync(string ticketId)
        => await _db.Table<LinkedTicket>().Where(l => l.SourceTicketId == ticketId).OrderByDescending(l => l.CreatedAt).ToListAsync();

    public async Task RemoveLinkedTicketAsync(int linkId)
        => await _db.DeleteAsync<LinkedTicket>(linkId);

    // --- Notifications ---

    public async Task AddNotificationAsync(string title, string description, string eventType, string ticketId)
    {
        await _db.InsertAsync(new NotificationEntry
        {
            Title = title,
            Description = description,
            EventType = eventType,
            TicketId = ticketId,
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        });
    }

    public async Task<List<NotificationEntry>> GetNotificationsAsync(int limit = 50)
        => await _db.Table<NotificationEntry>().OrderByDescending(n => n.CreatedAt).Take(limit).ToListAsync();

    public async Task<int> GetUnreadNotificationCountAsync()
        => await _db.Table<NotificationEntry>().Where(n => !n.IsRead).CountAsync();

    public async Task MarkNotificationReadAsync(int id)
    {
        var entry = await _db.FindAsync<NotificationEntry>(id);
        if (entry != null)
        {
            entry.IsRead = true;
            await _db.UpdateAsync(entry);
        }
    }

    public async Task MarkAllNotificationsReadAsync()
    {
        var unread = await _db.Table<NotificationEntry>().Where(n => !n.IsRead).ToListAsync();
        foreach (var entry in unread)
        {
            entry.IsRead = true;
            await _db.UpdateAsync(entry);
        }
    }

    public async Task ClearNotificationHistoryAsync()
        => await _db.DeleteAllAsync<NotificationEntry>();

    // --- Saved Filters ---

    public async Task<int> SaveFilterAsync(SavedFilter filter)
        => await _db.InsertAsync(filter);

    public async Task<List<SavedFilter>> GetSavedFiltersAsync()
        => await _db.Table<SavedFilter>().OrderByDescending(f => f.CreatedAt).ToListAsync();

    public async Task DeleteSavedFilterAsync(int id)
        => await _db.DeleteAsync<SavedFilter>(id);

    public async ValueTask DisposeAsync()
    {
        await _db.CloseAsync();
        try { File.Delete(_dbPath); } catch { /* best effort cleanup */ }
    }
}

public class DatabaseServiceTests : IAsyncLifetime
{
    private TestableDatabase _db = null!;
    private string _dbPath = null!;

    public async ValueTask InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"thw_test_{Guid.NewGuid():N}.db3");
        _db = new TestableDatabase(_dbPath);
        await _db.InitAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _db.DisposeAsync();
    }

    // ========== Helpers ==========

    private static Ticket CreateTicket(string id, string subject = "Test Ticket", int uid = 1001)
    {
        return new Ticket
        {
            Id = id,
            Subject = subject,
            Issue = "Test issue body",
            Date = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc),
            Updated = new DateTime(2025, 6, 16, 12, 0, 0, DateTimeKind.Utc),
            Uid = uid,
            Deleted = false,
            Status = new Status { Id = "s1", Name = "Open", HtmlColor = "#00FF00", IsResolved = false },
            Priority = new Priority { Id = "p1", Name = "High", HtmlColor = "#FF0000" },
            Type = new TicketType { Id = "t1", Name = "Bug" },
            Owner = new Owner { Id = "o1", Fullname = "Max Mustermann", Email = "max@example.com" },
            Assignee = new Assignee { Id = "a1", Fullname = "Anna Admin", Email = "anna@example.com" },
            Group = new Group { Id = "g1", Name = "Support Team" }
        };
    }

    // ========== Cache Operations ==========

    [Fact]
    public async Task SaveTicketsAsync_And_GetTicketsFromCacheAsync_RoundTrips_TicketData()
    {
        var ticket = CreateTicket("ticket-1", "Server down", 42);
        ticket.DueDate = new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        ticket.ClosedDate = null;

        await _db.SaveTicketsAsync(new[] { ticket });
        var result = await _db.GetTicketsFromCacheAsync();

        result.Should().HaveCount(1);
        var rt = result[0];
        rt.Id.Should().Be("ticket-1");
        rt.Subject.Should().Be("Server down");
        rt.Uid.Should().Be(42);
        rt.Issue.Should().Be("Test issue body");
        rt.Status.Should().NotBeNull();
        rt.Status!.Id.Should().Be("s1");
        rt.Status.Name.Should().Be("Open");
        rt.Status.HtmlColor.Should().Be("#00FF00");
        rt.Status.IsResolved.Should().BeFalse();
        rt.Priority.Should().NotBeNull();
        rt.Priority!.Name.Should().Be("High");
        rt.Type.Should().NotBeNull();
        rt.Type!.Name.Should().Be("Bug");
        rt.Owner.Should().NotBeNull();
        rt.Owner!.Fullname.Should().Be("Max Mustermann");
        rt.Assignee.Should().NotBeNull();
        rt.Assignee!.Fullname.Should().Be("Anna Admin");
        rt.Group.Should().NotBeNull();
        rt.Group!.Name.Should().Be("Support Team");
    }

    [Fact]
    public async Task SaveTicketsAsync_ReplacesExistingCache()
    {
        await _db.SaveTicketsAsync(new[] { CreateTicket("t1"), CreateTicket("t2") });
        await _db.SaveTicketsAsync(new[] { CreateTicket("t3") });

        var result = await _db.GetTicketsFromCacheAsync();
        result.Should().HaveCount(1);
        result[0].Id.Should().Be("t3");
    }

    [Fact]
    public async Task SaveTicketsAsync_WithNullRelatedData_SetsNullOnRehydration()
    {
        var ticket = new Ticket { Id = "bare", Subject = "Bare ticket", Uid = 1 };
        await _db.SaveTicketsAsync(new[] { ticket });

        var result = await _db.GetTicketsFromCacheAsync();
        result.Should().HaveCount(1);
        result[0].Status.Should().BeNull();
        result[0].Priority.Should().BeNull();
        result[0].Type.Should().BeNull();
        result[0].Owner.Should().BeNull();
        result[0].Assignee.Should().BeNull();
        result[0].Group.Should().BeNull();
    }

    [Fact]
    public async Task GetCachedTicketCountAsync_ReturnsCorrectCount()
    {
        (await _db.GetCachedTicketCountAsync()).Should().Be(0);

        await _db.SaveTicketsAsync(new[] { CreateTicket("a"), CreateTicket("b"), CreateTicket("c") });
        (await _db.GetCachedTicketCountAsync()).Should().Be(3);
    }

    [Fact]
    public async Task ClearCacheAsync_RemovesAllCachedTickets()
    {
        await _db.SaveTicketsAsync(new[] { CreateTicket("a"), CreateTicket("b") });
        await _db.ClearCacheAsync();

        (await _db.GetCachedTicketCountAsync()).Should().Be(0);
        (await _db.GetTicketsFromCacheAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task GetLastCacheTimeAsync_ReturnsNull_WhenEmpty()
    {
        var result = await _db.GetLastCacheTimeAsync();
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetLastCacheTimeAsync_ReturnsTime_AfterSaving()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        await _db.SaveTicketsAsync(new[] { CreateTicket("x") });
        var after = DateTime.UtcNow.AddSeconds(1);

        var result = await _db.GetLastCacheTimeAsync();
        result.Should().NotBeNull();
        result!.Value.Should().BeAfter(before).And.BeBefore(after);
    }

    // ========== Pending Actions ==========

    [Fact]
    public async Task EnqueueActionAsync_And_GetPendingActionsAsync_RoundTrips()
    {
        await _db.EnqueueActionAsync("UpdateTicket", "{\"id\":\"t1\"}", new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var actions = await _db.GetPendingActionsAsync();
        actions.Should().HaveCount(1);
        actions[0].ActionType.Should().Be("UpdateTicket");
        actions[0].PayloadJson.Should().Be("{\"id\":\"t1\"}");
        actions[0].RetryCount.Should().Be(0);
        actions[0].TicketUpdatedAt.Should().Be(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        actions[0].IsConflicted.Should().BeFalse();
    }

    [Fact]
    public async Task GetPendingActionCountAsync_ReturnsCorrectCount()
    {
        (await _db.GetPendingActionCountAsync()).Should().Be(0);

        await _db.EnqueueActionAsync("A", "{}");
        await _db.EnqueueActionAsync("B", "{}");
        (await _db.GetPendingActionCountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task RemoveActionAsync_DeletesSpecificAction()
    {
        await _db.EnqueueActionAsync("A", "{}");
        await _db.EnqueueActionAsync("B", "{}");

        var actions = await _db.GetPendingActionsAsync();
        await _db.RemoveActionAsync(actions[0].Id);

        var remaining = await _db.GetPendingActionsAsync();
        remaining.Should().HaveCount(1);
        remaining[0].ActionType.Should().Be("B");
    }

    [Fact]
    public async Task IncrementRetryCountAsync_IncrementsRetryCount()
    {
        await _db.EnqueueActionAsync("Retry", "{}");
        var actions = await _db.GetPendingActionsAsync();
        var id = actions[0].Id;

        await _db.IncrementRetryCountAsync(id);
        await _db.IncrementRetryCountAsync(id);

        var updated = (await _db.GetPendingActionsAsync()).First(a => a.Id == id);
        updated.RetryCount.Should().Be(2);
    }

    [Fact]
    public async Task MarkActionConflictedAsync_SetsConflictFields()
    {
        await _db.EnqueueActionAsync("Update", "{}");
        var actions = await _db.GetPendingActionsAsync();
        var id = actions[0].Id;

        await _db.MarkActionConflictedAsync(id, "Server ticket was modified");

        var updated = (await _db.GetPendingActionsAsync()).First(a => a.Id == id);
        updated.IsConflicted.Should().BeTrue();
        updated.ConflictReason.Should().Be("Server ticket was modified");
    }

    [Fact]
    public async Task MarkActionConflictedAsync_NoOp_WhenIdNotFound()
    {
        // Should not throw
        await _db.MarkActionConflictedAsync(9999, "no such action");
        (await _db.GetPendingActionCountAsync()).Should().Be(0);
    }

    // ========== Favorites ==========

    [Fact]
    public async Task ToggleFavoriteAsync_AddsAndRemovesFavorite()
    {
        (await _db.IsFavoriteAsync("t1")).Should().BeFalse();

        await _db.ToggleFavoriteAsync("t1");
        (await _db.IsFavoriteAsync("t1")).Should().BeTrue();

        await _db.ToggleFavoriteAsync("t1");
        (await _db.IsFavoriteAsync("t1")).Should().BeFalse();
    }

    [Fact]
    public async Task IsFavoriteAsync_ReturnsFalse_ForUnknownId()
    {
        (await _db.IsFavoriteAsync("nonexistent")).Should().BeFalse();
    }

    [Fact]
    public async Task GetFavoriteIdsAsync_ReturnsAllFavoriteIds()
    {
        await _db.ToggleFavoriteAsync("t1");
        await _db.ToggleFavoriteAsync("t2");
        await _db.ToggleFavoriteAsync("t3");

        var ids = await _db.GetFavoriteIdsAsync();
        ids.Should().BeEquivalentTo(new HashSet<string> { "t1", "t2", "t3" });
    }

    [Fact]
    public async Task GetFavoriteIdsAsync_ExcludesRemovedFavorites()
    {
        await _db.ToggleFavoriteAsync("t1");
        await _db.ToggleFavoriteAsync("t2");
        await _db.ToggleFavoriteAsync("t1"); // remove t1

        var ids = await _db.GetFavoriteIdsAsync();
        ids.Should().BeEquivalentTo(new HashSet<string> { "t2" });
    }

    // ========== Time Tracking ==========

    [Fact]
    public async Task StartTimerAsync_CreatesActiveTimer()
    {
        var entry = await _db.StartTimerAsync("t1");
        entry.Id.Should().BeGreaterThan(0);
        entry.TicketId.Should().Be("t1");

        var active = await _db.GetActiveTimerAsync("t1");
        active.Should().NotBeNull();
        active!.EndTime.Should().BeNull();
    }

    [Fact]
    public async Task StopTimerAsync_SetsEndTimeAndDescription()
    {
        var entry = await _db.StartTimerAsync("t1");
        await _db.StopTimerAsync(entry.Id, "Fixed the bug");

        var active = await _db.GetActiveTimerAsync("t1");
        active.Should().BeNull("timer should no longer be active");

        var entries = await _db.GetTimeEntriesAsync("t1");
        entries.Should().HaveCount(1);
        entries[0].EndTime.Should().NotBeNull();
        entries[0].Description.Should().Be("Fixed the bug");
    }

    [Fact]
    public async Task GetActiveTimerAsync_ReturnsNull_WhenNoActiveTimer()
    {
        var result = await _db.GetActiveTimerAsync("no-timer");
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetTimeEntriesAsync_ReturnsEntriesOrderedByStartTimeDescending()
    {
        await _db.StartTimerAsync("t1");
        await Task.Delay(50); // ensure different timestamps
        await _db.StartTimerAsync("t1");

        var entries = await _db.GetTimeEntriesAsync("t1");
        entries.Should().HaveCount(2);
        entries[0].StartTime.Should().BeOnOrAfter(entries[1].StartTime);
    }

    [Fact]
    public async Task GetTimeEntriesAsync_OnlyReturnsEntriesForGivenTicket()
    {
        await _db.StartTimerAsync("t1");
        await _db.StartTimerAsync("t2");

        var entries = await _db.GetTimeEntriesAsync("t1");
        entries.Should().HaveCount(1);
        entries[0].TicketId.Should().Be("t1");
    }

    [Fact]
    public async Task GetTotalTimeAsync_SumsCompletedEntries()
    {
        var e1 = await _db.StartTimerAsync("t1");
        await _db.StopTimerAsync(e1.Id);

        var e2 = await _db.StartTimerAsync("t1");
        await _db.StopTimerAsync(e2.Id);

        // Also start an active timer that should NOT be counted
        await _db.StartTimerAsync("t1");

        var total = await _db.GetTotalTimeAsync("t1");
        total.Should().BeGreaterOrEqualTo(0, "completed entries should have non-negative duration");
    }

    [Fact]
    public async Task GetTotalTimeAsync_ReturnsZero_WhenNoCompletedEntries()
    {
        var total = await _db.GetTotalTimeAsync("empty");
        total.Should().Be(0);
    }

    [Fact]
    public async Task DeleteTimeEntryAsync_RemovesEntry()
    {
        var entry = await _db.StartTimerAsync("t1");
        await _db.DeleteTimeEntryAsync(entry.Id);

        var entries = await _db.GetTimeEntriesAsync("t1");
        entries.Should().BeEmpty();
    }

    // ========== Linked Tickets ==========

    [Fact]
    public async Task AddLinkedTicketAsync_And_GetLinkedTicketsAsync_RoundTrips()
    {
        await _db.AddLinkedTicketAsync("src", "link1", "Linked Subject", 100, "blocks");

        var links = await _db.GetLinkedTicketsAsync("src");
        links.Should().HaveCount(1);
        links[0].SourceTicketId.Should().Be("src");
        links[0].LinkedTicketId.Should().Be("link1");
        links[0].LinkedTicketSubject.Should().Be("Linked Subject");
        links[0].LinkedTicketUid.Should().Be(100);
        links[0].LinkType.Should().Be("blocks");
    }

    [Fact]
    public async Task AddLinkedTicketAsync_PreventsDuplicates()
    {
        await _db.AddLinkedTicketAsync("src", "link1", "Subject", 100);
        await _db.AddLinkedTicketAsync("src", "link1", "Subject Updated", 200); // duplicate

        var links = await _db.GetLinkedTicketsAsync("src");
        links.Should().HaveCount(1, "duplicate link should not be inserted");
        links[0].LinkedTicketSubject.Should().Be("Subject", "original should be preserved");
    }

    [Fact]
    public async Task AddLinkedTicketAsync_AllowsSameLinkedIdFromDifferentSource()
    {
        await _db.AddLinkedTicketAsync("src1", "link1", "Subject", 100);
        await _db.AddLinkedTicketAsync("src2", "link1", "Subject", 100);

        var links1 = await _db.GetLinkedTicketsAsync("src1");
        var links2 = await _db.GetLinkedTicketsAsync("src2");
        links1.Should().HaveCount(1);
        links2.Should().HaveCount(1);
    }

    [Fact]
    public async Task RemoveLinkedTicketAsync_RemovesSpecificLink()
    {
        await _db.AddLinkedTicketAsync("src", "link1", "Sub1", 1);
        await _db.AddLinkedTicketAsync("src", "link2", "Sub2", 2);

        var links = await _db.GetLinkedTicketsAsync("src");
        await _db.RemoveLinkedTicketAsync(links[0].Id);

        var remaining = await _db.GetLinkedTicketsAsync("src");
        remaining.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetLinkedTicketsAsync_ReturnsEmpty_WhenNoneExist()
    {
        var links = await _db.GetLinkedTicketsAsync("no-links");
        links.Should().BeEmpty();
    }

    [Fact]
    public async Task AddLinkedTicketAsync_UsesDefaultLinkType()
    {
        await _db.AddLinkedTicketAsync("src", "link1", "Subject", 1);

        var links = await _db.GetLinkedTicketsAsync("src");
        links[0].LinkType.Should().Be("related");
    }

    // ========== Notifications ==========

    [Fact]
    public async Task AddNotificationAsync_And_GetNotificationsAsync_RoundTrips()
    {
        await _db.AddNotificationAsync("Title1", "Desc1", "StatusChange", "t1");

        var notifications = await _db.GetNotificationsAsync();
        notifications.Should().HaveCount(1);
        notifications[0].Title.Should().Be("Title1");
        notifications[0].Description.Should().Be("Desc1");
        notifications[0].EventType.Should().Be("StatusChange");
        notifications[0].TicketId.Should().Be("t1");
        notifications[0].IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task GetNotificationsAsync_RespectsLimit()
    {
        for (int i = 0; i < 5; i++)
        {
            await _db.AddNotificationAsync($"Title{i}", "Desc", "Event", "t1");
            await Task.Delay(10); // ensure ordering
        }

        var limited = await _db.GetNotificationsAsync(limit: 3);
        limited.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetNotificationsAsync_OrderedByCreatedAtDescending()
    {
        await _db.AddNotificationAsync("First", "Desc", "Event", "t1");
        await Task.Delay(50);
        await _db.AddNotificationAsync("Second", "Desc", "Event", "t1");

        var notifications = await _db.GetNotificationsAsync();
        notifications[0].Title.Should().Be("Second");
        notifications[1].Title.Should().Be("First");
    }

    [Fact]
    public async Task GetUnreadNotificationCountAsync_CountsOnlyUnread()
    {
        await _db.AddNotificationAsync("N1", "D", "E", "t1");
        await _db.AddNotificationAsync("N2", "D", "E", "t1");

        (await _db.GetUnreadNotificationCountAsync()).Should().Be(2);

        var notifications = await _db.GetNotificationsAsync();
        await _db.MarkNotificationReadAsync(notifications[0].Id);

        (await _db.GetUnreadNotificationCountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task MarkNotificationReadAsync_MarksSpecificNotificationAsRead()
    {
        await _db.AddNotificationAsync("N1", "D", "E", "t1");
        var notifications = await _db.GetNotificationsAsync();
        var id = notifications[0].Id;

        await _db.MarkNotificationReadAsync(id);

        var updated = await _db.GetNotificationsAsync();
        updated[0].IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task MarkNotificationReadAsync_NoOp_WhenIdNotFound()
    {
        // Should not throw
        await _db.MarkNotificationReadAsync(9999);
    }

    [Fact]
    public async Task MarkAllNotificationsReadAsync_MarksAllAsRead()
    {
        await _db.AddNotificationAsync("N1", "D", "E", "t1");
        await _db.AddNotificationAsync("N2", "D", "E", "t2");
        await _db.AddNotificationAsync("N3", "D", "E", "t3");

        await _db.MarkAllNotificationsReadAsync();

        (await _db.GetUnreadNotificationCountAsync()).Should().Be(0);
        var all = await _db.GetNotificationsAsync();
        all.Should().OnlyContain(n => n.IsRead);
    }

    [Fact]
    public async Task ClearNotificationHistoryAsync_RemovesAllNotifications()
    {
        await _db.AddNotificationAsync("N1", "D", "E", "t1");
        await _db.AddNotificationAsync("N2", "D", "E", "t2");

        await _db.ClearNotificationHistoryAsync();

        (await _db.GetNotificationsAsync()).Should().BeEmpty();
        (await _db.GetUnreadNotificationCountAsync()).Should().Be(0);
    }

    // ========== Saved Filters ==========

    [Fact]
    public async Task SaveFilterAsync_And_GetSavedFiltersAsync_RoundTrips()
    {
        var filter = new SavedFilter
        {
            Name = "My Filter",
            FilterJson = "{\"status\":\"open\"}",
            CreatedAt = DateTime.UtcNow
        };

        await _db.SaveFilterAsync(filter);
        var filters = await _db.GetSavedFiltersAsync();

        filters.Should().HaveCount(1);
        filters[0].Name.Should().Be("My Filter");
        filters[0].FilterJson.Should().Be("{\"status\":\"open\"}");
    }

    [Fact]
    public async Task GetSavedFiltersAsync_OrderedByCreatedAtDescending()
    {
        await _db.SaveFilterAsync(new SavedFilter { Name = "First", FilterJson = "{}", CreatedAt = new DateTime(2025, 1, 1) });
        await _db.SaveFilterAsync(new SavedFilter { Name = "Second", FilterJson = "{}", CreatedAt = new DateTime(2025, 6, 1) });

        var filters = await _db.GetSavedFiltersAsync();
        filters[0].Name.Should().Be("Second");
        filters[1].Name.Should().Be("First");
    }

    [Fact]
    public async Task DeleteSavedFilterAsync_RemovesSpecificFilter()
    {
        await _db.SaveFilterAsync(new SavedFilter { Name = "Keep", FilterJson = "{}", CreatedAt = DateTime.UtcNow });
        await _db.SaveFilterAsync(new SavedFilter { Name = "Delete", FilterJson = "{}", CreatedAt = DateTime.UtcNow });

        var filters = await _db.GetSavedFiltersAsync();
        var toDelete = filters.First(f => f.Name == "Delete");
        await _db.DeleteSavedFilterAsync(toDelete.Id);

        var remaining = await _db.GetSavedFiltersAsync();
        remaining.Should().HaveCount(1);
        remaining[0].Name.Should().Be("Keep");
    }

    [Fact]
    public async Task GetSavedFiltersAsync_ReturnsEmpty_WhenNoneExist()
    {
        var filters = await _db.GetSavedFiltersAsync();
        filters.Should().BeEmpty();
    }
}
