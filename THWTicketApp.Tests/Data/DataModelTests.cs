using FluentAssertions;
using THWTicketApp.Data;
using Xunit;

namespace THWTicketApp.Tests.Data;

public class CachedTicketTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var ct = new CachedTicket();

        ct.Id.Should().BeEmpty();
        ct.Subject.Should().BeNull();
        ct.Issue.Should().BeNull();
        ct.Uid.Should().Be(0);
        ct.Deleted.Should().BeFalse();
        ct.DueDate.Should().BeNull();
        ct.ClosedDate.Should().BeNull();
        ct.StatusId.Should().BeNull();
        ct.StatusName.Should().BeNull();
        ct.StatusHtmlColor.Should().BeNull();
        ct.StatusIsResolved.Should().BeFalse();
        ct.PriorityId.Should().BeNull();
        ct.PriorityName.Should().BeNull();
        ct.PriorityHtmlColor.Should().BeNull();
        ct.TypeId.Should().BeNull();
        ct.TypeName.Should().BeNull();
        ct.OwnerId.Should().BeNull();
        ct.OwnerFullname.Should().BeNull();
        ct.OwnerEmail.Should().BeNull();
        ct.AssigneeId.Should().BeNull();
        ct.AssigneeFullname.Should().BeNull();
        ct.AssigneeEmail.Should().BeNull();
        ct.GroupId.Should().BeNull();
        ct.GroupName.Should().BeNull();
    }

    [Fact]
    public void SetProperties_PersistValues()
    {
        var now = DateTime.UtcNow;
        var ct = new CachedTicket
        {
            Id = "ticket-1",
            Subject = "Server down",
            Issue = "Cannot reach server",
            Uid = 100,
            Deleted = false,
            Date = now,
            Updated = now,
            StatusId = "s1",
            StatusName = "Open",
            StatusHtmlColor = "#29b955",
            StatusIsResolved = false,
            PriorityId = "p1",
            PriorityName = "High",
            PriorityHtmlColor = "#e74c3c",
            OwnerId = "o1",
            OwnerFullname = "John Doe",
            GroupId = "g1",
            GroupName = "IT",
            CachedAt = now
        };

        ct.Id.Should().Be("ticket-1");
        ct.Subject.Should().Be("Server down");
        ct.Uid.Should().Be(100);
        ct.StatusName.Should().Be("Open");
        ct.PriorityName.Should().Be("High");
        ct.OwnerFullname.Should().Be("John Doe");
        ct.GroupName.Should().Be("IT");
        ct.CachedAt.Should().Be(now);
    }
}

public class PendingActionTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var pa = new PendingAction();

        pa.Id.Should().Be(0);
        pa.ActionType.Should().BeEmpty();
        pa.PayloadJson.Should().BeEmpty();
        pa.RetryCount.Should().Be(0);
        pa.TicketUpdatedAt.Should().BeNull();
        pa.IsConflicted.Should().BeFalse();
        pa.ConflictReason.Should().BeNull();
    }

    [Fact]
    public void SetProperties_PersistValues()
    {
        var now = DateTime.UtcNow;
        var pa = new PendingAction
        {
            Id = 5,
            ActionType = "UpdateStatus",
            PayloadJson = "{\"status\":\"closed\"}",
            CreatedAt = now,
            RetryCount = 3,
            TicketUpdatedAt = now.AddHours(-1),
            IsConflicted = true,
            ConflictReason = "Server ticket modified since queuing"
        };

        pa.Id.Should().Be(5);
        pa.ActionType.Should().Be("UpdateStatus");
        pa.PayloadJson.Should().Contain("closed");
        pa.RetryCount.Should().Be(3);
        pa.IsConflicted.Should().BeTrue();
        pa.ConflictReason.Should().NotBeNull();
        pa.TicketUpdatedAt.Should().NotBeNull();
    }
}

public class FavoriteTicketTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var ft = new FavoriteTicket();

        ft.TicketId.Should().BeEmpty();
        ft.AddedAt.Should().Be(default(DateTime));
    }

    [Fact]
    public void SetProperties_PersistValues()
    {
        var now = DateTime.UtcNow;
        var ft = new FavoriteTicket
        {
            TicketId = "ticket-42",
            AddedAt = now
        };

        ft.TicketId.Should().Be("ticket-42");
        ft.AddedAt.Should().Be(now);
    }
}

public class TimeEntryTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var te = new TimeEntry();

        te.Id.Should().Be(0);
        te.TicketId.Should().BeEmpty();
        te.EndTime.Should().BeNull();
        te.Description.Should().BeNull();
    }

    [Fact]
    public void DurationMinutes_WithEndTime_CalculatesCorrectly()
    {
        var start = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2025, 6, 15, 11, 30, 0, DateTimeKind.Utc);

        var te = new TimeEntry
        {
            TicketId = "t1",
            StartTime = start,
            EndTime = end
        };

        te.DurationMinutes.Should().Be(90.0);
    }

    [Fact]
    public void DurationMinutes_WithEndTime_ZeroDuration()
    {
        var time = new DateTime(2025, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var te = new TimeEntry
        {
            StartTime = time,
            EndTime = time
        };

        te.DurationMinutes.Should().Be(0.0);
    }

    [Fact]
    public void DurationMinutes_WithoutEndTime_UsesCurrentTime()
    {
        var te = new TimeEntry
        {
            StartTime = DateTime.UtcNow.AddMinutes(-10)
        };

        // Without EndTime, duration should be roughly 10 minutes (allow some tolerance)
        te.DurationMinutes.Should().BeApproximately(10.0, 1.0);
    }

    [Fact]
    public void DurationMinutes_FractionalMinutes()
    {
        var start = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2025, 1, 1, 0, 0, 30, DateTimeKind.Utc); // 30 seconds

        var te = new TimeEntry { StartTime = start, EndTime = end };

        te.DurationMinutes.Should().Be(0.5);
    }
}

public class LinkedTicketTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var lt = new LinkedTicket();

        lt.Id.Should().Be(0);
        lt.SourceTicketId.Should().BeEmpty();
        lt.LinkedTicketId.Should().BeEmpty();
        lt.LinkedTicketSubject.Should().BeEmpty();
        lt.LinkedTicketUid.Should().Be(0);
        lt.LinkType.Should().Be("related");
    }

    [Theory]
    [InlineData("related")]
    [InlineData("blocks")]
    [InlineData("blocked_by")]
    [InlineData("duplicate")]
    public void LinkType_AcceptsValidValues(string linkType)
    {
        var lt = new LinkedTicket { LinkType = linkType };
        lt.LinkType.Should().Be(linkType);
    }

    [Fact]
    public void SetProperties_PersistValues()
    {
        var now = DateTime.UtcNow;
        var lt = new LinkedTicket
        {
            Id = 1,
            SourceTicketId = "src-1",
            LinkedTicketId = "linked-1",
            LinkedTicketSubject = "Related issue",
            LinkedTicketUid = 55,
            LinkType = "blocks",
            CreatedAt = now
        };

        lt.SourceTicketId.Should().Be("src-1");
        lt.LinkedTicketId.Should().Be("linked-1");
        lt.LinkedTicketSubject.Should().Be("Related issue");
        lt.LinkedTicketUid.Should().Be(55);
        lt.LinkType.Should().Be("blocks");
        lt.CreatedAt.Should().Be(now);
    }
}

public class NotificationEntryTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var ne = new NotificationEntry();

        ne.Id.Should().Be(0);
        ne.Title.Should().BeEmpty();
        ne.Description.Should().BeEmpty();
        ne.EventType.Should().BeEmpty();
        ne.TicketId.Should().BeEmpty();
        ne.IsRead.Should().BeFalse();
    }

    [Fact]
    public void SetProperties_PersistValues()
    {
        var now = DateTime.UtcNow;
        var ne = new NotificationEntry
        {
            Id = 10,
            Title = "Ticket Updated",
            Description = "Status changed to Closed",
            EventType = "status_change",
            TicketId = "t-100",
            CreatedAt = now,
            IsRead = true
        };

        ne.Id.Should().Be(10);
        ne.Title.Should().Be("Ticket Updated");
        ne.Description.Should().Be("Status changed to Closed");
        ne.EventType.Should().Be("status_change");
        ne.TicketId.Should().Be("t-100");
        ne.CreatedAt.Should().Be(now);
        ne.IsRead.Should().BeTrue();
    }
}

public class SavedFilterTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var sf = new SavedFilter();

        sf.Id.Should().Be(0);
        sf.Name.Should().BeEmpty();
        sf.FilterJson.Should().BeEmpty();
    }

    [Fact]
    public void SetProperties_PersistValues()
    {
        var now = DateTime.UtcNow;
        var sf = new SavedFilter
        {
            Id = 1,
            Name = "Open High Priority",
            FilterJson = "{\"status\":\"Open\",\"priority\":\"High\"}",
            CreatedAt = now
        };

        sf.Id.Should().Be(1);
        sf.Name.Should().Be("Open High Priority");
        sf.FilterJson.Should().Contain("Open");
        sf.FilterJson.Should().Contain("High");
        sf.CreatedAt.Should().Be(now);
    }

    [Fact]
    public void FilterJson_CanStoreComplexFilter()
    {
        var complexJson = "{\"statuses\":[\"Open\",\"Pending\"],\"priorities\":[\"High\",\"Critical\"],\"groups\":[\"IT\"]}";
        var sf = new SavedFilter
        {
            Name = "Complex Filter",
            FilterJson = complexJson
        };

        sf.FilterJson.Should().Be(complexJson);
    }
}
