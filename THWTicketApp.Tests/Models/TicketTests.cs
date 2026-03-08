using System.Text.Json;
using FluentAssertions;
using THWTicketApp.Models;
using Xunit;

namespace THWTicketApp.Tests.Models;

public class TicketTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void Deserialize_FullTicket_MapsAllFields()
    {
        var json = """
        {
            "_id": "abc123",
            "subject": "Test Ticket",
            "issue": "<p>Description here</p>",
            "uid": 42,
            "date": "2025-06-15T10:30:00Z",
            "deleted": false,
            "updated": "2025-06-16T08:00:00Z",
            "dueDate": "2025-07-01T00:00:00Z",
            "closedDate": "2025-06-20T12:00:00Z",
            "__v": 3,
            "status": {
                "_id": "status1",
                "name": "Open",
                "htmlColor": "#29b955",
                "uid": 1,
                "order": 1,
                "slatimer": true,
                "isResolved": false,
                "isLocked": false,
                "__v": 0
            },
            "priority": {
                "_id": "prio1",
                "name": "High",
                "htmlColor": "#e74c3c",
                "overdueIn": 48,
                "__v": 0
            },
            "group": {
                "_id": "grp1",
                "name": "IT Support",
                "public": true,
                "__v": 0
            },
            "owner": {
                "_id": "owner1",
                "username": "jdoe",
                "fullname": "John Doe",
                "email": "jdoe@example.com"
            },
            "assignee": {
                "_id": "assign1",
                "username": "jsmith",
                "fullname": "Jane Smith",
                "email": "jsmith@example.com"
            }
        }
        """;

        var ticket = JsonSerializer.Deserialize<Ticket>(json, JsonOptions);

        ticket.Should().NotBeNull();
        ticket!.Id.Should().Be("abc123");
        ticket.Subject.Should().Be("Test Ticket");
        ticket.Issue.Should().Be("<p>Description here</p>");
        ticket.Uid.Should().Be(42);
        ticket.Date.Should().Be(new DateTime(2025, 6, 15, 10, 30, 0, DateTimeKind.Utc));
        ticket.Deleted.Should().BeFalse();
        ticket.Updated.Should().Be(new DateTime(2025, 6, 16, 8, 0, 0, DateTimeKind.Utc));
        ticket.Version.Should().Be(3);
        ticket.ClosedDate.Should().NotBeNull();

        ticket.Status.Should().NotBeNull();
        ticket.Status!.Id.Should().Be("status1");
        ticket.Status.Name.Should().Be("Open");
        ticket.Status.HtmlColor.Should().Be("#29b955");
        ticket.Status.IsResolved.Should().BeFalse();

        ticket.Priority.Should().NotBeNull();
        ticket.Priority!.Id.Should().Be("prio1");
        ticket.Priority.Name.Should().Be("High");

        ticket.Group.Should().NotBeNull();
        ticket.Group!.Id.Should().Be("grp1");
        ticket.Group.Name.Should().Be("IT Support");

        ticket.Owner.Should().NotBeNull();
        ticket.Owner!.Id.Should().Be("owner1");
        ticket.Owner.Fullname.Should().Be("John Doe");

        ticket.Assignee.Should().NotBeNull();
        ticket.Assignee!.Id.Should().Be("assign1");
        ticket.Assignee.Fullname.Should().Be("Jane Smith");
    }

    [Fact]
    public void Deserialize_PropertyNameCaseInsensitive_Works()
    {
        var json = """
        {
            "_id": "id1",
            "Subject": "Upper Case Subject",
            "UID": 99,
            "DELETED": true
        }
        """;

        var ticket = JsonSerializer.Deserialize<Ticket>(json, JsonOptions);

        ticket.Should().NotBeNull();
        ticket!.Id.Should().Be("id1");
        ticket.Subject.Should().Be("Upper Case Subject");
        ticket.Uid.Should().Be(99);
        ticket.Deleted.Should().BeTrue();
    }

    [Fact]
    public void Deserialize_MissingOptionalFields_DefaultsCorrectly()
    {
        var json = """
        {
            "_id": "minimal"
        }
        """;

        var ticket = JsonSerializer.Deserialize<Ticket>(json, JsonOptions);

        ticket.Should().NotBeNull();
        ticket!.Id.Should().Be("minimal");
        ticket.Subject.Should().BeNull();
        ticket.Issue.Should().BeNull();
        ticket.Status.Should().BeNull();
        ticket.Priority.Should().BeNull();
        ticket.Group.Should().BeNull();
        ticket.Owner.Should().BeNull();
        ticket.Assignee.Should().BeNull();
        ticket.ClosedDate.Should().BeNull();
        ticket.Tags.Should().BeEmpty();
        ticket.Comments.Should().BeEmpty();
        ticket.Notes.Should().BeEmpty();
        ticket.Attachments.Should().BeEmpty();
        ticket.History.Should().BeEmpty();
        ticket.Subscribers.Should().BeEmpty();
        ticket.Uid.Should().Be(0);
        ticket.Deleted.Should().BeFalse();
    }

    [Fact]
    public void Deserialize_NullJson_ReturnsNull()
    {
        var ticket = JsonSerializer.Deserialize<Ticket>("null", JsonOptions);
        ticket.Should().BeNull();
    }

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var ticket = new Ticket();

        ticket.Id.Should().BeEmpty();
        ticket.Tags.Should().NotBeNull().And.BeEmpty();
        ticket.Subscribers.Should().NotBeNull().And.BeEmpty();
        ticket.Comments.Should().NotBeNull().And.BeEmpty();
        ticket.Notes.Should().NotBeNull().And.BeEmpty();
        ticket.Attachments.Should().NotBeNull().And.BeEmpty();
        ticket.History.Should().NotBeNull().And.BeEmpty();
    }
}
