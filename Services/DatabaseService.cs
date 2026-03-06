using SQLite;
using THWTicketApp.Data;
using THWTicketApp.Models;

namespace THWTicketApp.Services;

public class DatabaseService
{
    private SQLiteAsyncConnection? _database;
    private readonly string _dbPath;

    public DatabaseService()
    {
        _dbPath = Path.Combine(FileSystem.AppDataDirectory, "thwtickets.db3");
    }

    private async Task InitAsync()
    {
        if (_database != null)
            return;

        _database = new SQLiteAsyncConnection(_dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
        await _database.CreateTableAsync<CachedTicket>();
        await _database.CreateTableAsync<PendingAction>();
        await _database.CreateTableAsync<FavoriteTicket>();
        await _database.CreateTableAsync<TimeEntry>();
        await _database.CreateTableAsync<LinkedTicket>();
    }

    public async Task<List<CachedTicket>> GetCachedTicketsAsync()
    {
        await InitAsync();
        return await _database!.Table<CachedTicket>()
            .OrderByDescending(t => t.Date)
            .ToListAsync();
    }

    public async Task<CachedTicket?> GetCachedTicketAsync(string id)
    {
        await InitAsync();
        return await _database!.Table<CachedTicket>()
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task SaveTicketsAsync(IEnumerable<Ticket> tickets)
    {
        await InitAsync();

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

        // Clear existing and insert new
        await _database!.DeleteAllAsync<CachedTicket>();
        await _database.InsertAllAsync(cachedTickets);
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

            Status = c.StatusId != null ? new Status
            {
                Id = c.StatusId,
                Name = c.StatusName,
                HtmlColor = c.StatusHtmlColor,
                IsResolved = c.StatusIsResolved
            } : null,

            Priority = c.PriorityId != null ? new Priority
            {
                Id = c.PriorityId,
                Name = c.PriorityName,
                HtmlColor = c.PriorityHtmlColor
            } : null,

            Type = c.TypeId != null ? new TicketType
            {
                Id = c.TypeId,
                Name = c.TypeName
            } : null,

            Owner = c.OwnerId != null ? new Owner
            {
                Id = c.OwnerId,
                Fullname = c.OwnerFullname,
                Email = c.OwnerEmail
            } : null,

            Assignee = c.AssigneeId != null ? new Assignee
            {
                Id = c.AssigneeId,
                Fullname = c.AssigneeFullname,
                Email = c.AssigneeEmail
            } : null,

            Group = c.GroupId != null ? new Group
            {
                Id = c.GroupId,
                Name = c.GroupName
            } : null
        }).ToList();
    }

    public async Task ClearCacheAsync()
    {
        await InitAsync();
        await _database!.DeleteAllAsync<CachedTicket>();
    }

    public async Task<DateTime?> GetLastCacheTimeAsync()
    {
        await InitAsync();
        var first = await _database!.Table<CachedTicket>().FirstOrDefaultAsync();
        return first?.CachedAt;
    }

    public async Task<int> GetCachedTicketCountAsync()
    {
        await InitAsync();
        return await _database!.Table<CachedTicket>().CountAsync();
    }

    // --- Pending Actions Queue ---

    public async Task EnqueueActionAsync(string actionType, string payloadJson)
    {
        await InitAsync();
        var action = new PendingAction
        {
            ActionType = actionType,
            PayloadJson = payloadJson,
            CreatedAt = DateTime.UtcNow,
            RetryCount = 0
        };
        await _database!.InsertAsync(action);
    }

    public async Task<List<PendingAction>> GetPendingActionsAsync()
    {
        await InitAsync();
        return await _database!.Table<PendingAction>()
            .OrderBy(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> GetPendingActionCountAsync()
    {
        await InitAsync();
        return await _database!.Table<PendingAction>().CountAsync();
    }

    public async Task RemoveActionAsync(int id)
    {
        await InitAsync();
        await _database!.DeleteAsync<PendingAction>(id);
    }

    public async Task IncrementRetryCountAsync(int id)
    {
        await InitAsync();
        var action = await _database!.FindAsync<PendingAction>(id);
        if (action != null)
        {
            action.RetryCount++;
            await _database.UpdateAsync(action);
        }
    }

    // --- Favorites ---

    public async Task<bool> IsFavoriteAsync(string ticketId)
    {
        await InitAsync();
        var fav = await _database!.Table<FavoriteTicket>()
            .FirstOrDefaultAsync(f => f.TicketId == ticketId);
        return fav != null;
    }

    public async Task ToggleFavoriteAsync(string ticketId)
    {
        await InitAsync();
        var existing = await _database!.Table<FavoriteTicket>()
            .FirstOrDefaultAsync(f => f.TicketId == ticketId);

        if (existing != null)
            await _database.DeleteAsync(existing);
        else
            await _database.InsertAsync(new FavoriteTicket
            {
                TicketId = ticketId,
                AddedAt = DateTime.UtcNow
            });
    }

    public async Task<HashSet<string>> GetFavoriteIdsAsync()
    {
        await InitAsync();
        var favorites = await _database!.Table<FavoriteTicket>().ToListAsync();
        return favorites.Select(f => f.TicketId).ToHashSet();
    }

    // --- Time Tracking ---

    public async Task<TimeEntry> StartTimerAsync(string ticketId)
    {
        await InitAsync();
        var entry = new TimeEntry
        {
            TicketId = ticketId,
            StartTime = DateTime.UtcNow
        };
        await _database!.InsertAsync(entry);
        return entry;
    }

    public async Task StopTimerAsync(int entryId, string? description = null)
    {
        await InitAsync();
        var entry = await _database!.FindAsync<TimeEntry>(entryId);
        if (entry != null)
        {
            entry.EndTime = DateTime.UtcNow;
            entry.Description = description;
            await _database.UpdateAsync(entry);
        }
    }

    public async Task<TimeEntry?> GetActiveTimerAsync(string ticketId)
    {
        await InitAsync();
        return await _database!.Table<TimeEntry>()
            .FirstOrDefaultAsync(t => t.TicketId == ticketId && t.EndTime == null);
    }

    public async Task<List<TimeEntry>> GetTimeEntriesAsync(string ticketId)
    {
        await InitAsync();
        return await _database!.Table<TimeEntry>()
            .Where(t => t.TicketId == ticketId)
            .OrderByDescending(t => t.StartTime)
            .ToListAsync();
    }

    public async Task<double> GetTotalTimeAsync(string ticketId)
    {
        await InitAsync();
        var entries = await _database!.Table<TimeEntry>()
            .Where(t => t.TicketId == ticketId && t.EndTime != null)
            .ToListAsync();
        return entries.Sum(e => e.DurationMinutes);
    }

    public async Task DeleteTimeEntryAsync(int entryId)
    {
        await InitAsync();
        await _database!.DeleteAsync<TimeEntry>(entryId);
    }

    // --- Linked Tickets ---

    public async Task AddLinkedTicketAsync(string sourceId, string linkedId, string linkedSubject, int linkedUid, string linkType = "related")
    {
        await InitAsync();
        var existing = await _database!.Table<LinkedTicket>()
            .FirstOrDefaultAsync(l => l.SourceTicketId == sourceId && l.LinkedTicketId == linkedId);
        if (existing != null) return;

        await _database.InsertAsync(new LinkedTicket
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
    {
        await InitAsync();
        return await _database!.Table<LinkedTicket>()
            .Where(l => l.SourceTicketId == ticketId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();
    }

    public async Task RemoveLinkedTicketAsync(int linkId)
    {
        await InitAsync();
        await _database!.DeleteAsync<LinkedTicket>(linkId);
    }
}
