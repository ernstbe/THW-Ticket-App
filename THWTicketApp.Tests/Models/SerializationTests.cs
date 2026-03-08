using System.Text.Json;
using FluentAssertions;
using THWTicketApp.Models;
using THWTicketApp.Models.Responses;
using Xunit;

namespace THWTicketApp.Tests.Models;

public class SerializationTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static T Deserialize<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, Options)!;

    #region Ticket

    [Fact]
    public void Ticket_Deserialize_FullJson_MapsAllProperties()
    {
        var json = """
        {
            "_id": "ticket001",
            "group": { "_id": "grp1", "name": "Support" },
            "deleted": false,
            "type": { "_id": "type1", "name": "Issue" },
            "priority": { "_id": "pri1", "name": "Normal", "htmlColor": "#ccc", "overdueIn": 48 },
            "tags": [{ "_id": "tag1", "name": "urgent", "normalized": "urgent" }],
            "subject": "Test Subject",
            "issue": "<p>Test Issue</p>",
            "subscribers": ["user1", "user2"],
            "date": "2025-01-15T10:30:00Z",
            "comments": [{ "_id": "c1", "comment": "A comment", "date": "2025-01-15T11:00:00Z" }],
            "notes": [{ "_id": "n1", "note": "A note", "date": "2025-01-15T11:30:00Z" }],
            "attachments": [{ "_id": "att1", "name": "file.txt", "path": "/uploads/file.txt", "type": "text/plain", "size": 1024 }],
            "history": [{ "_id": "h1", "action": "ticket:created", "description": "Ticket created", "date": "2025-01-15T10:30:00Z" }],
            "status": { "_id": "s1", "name": "Open", "htmlColor": "#29b955" },
            "owner": { "_id": "own1", "fullname": "John Doe", "email": "john@example.com", "username": "jdoe" },
            "uid": 1001,
            "__v": 3,
            "dueDate": "2025-01-20T00:00:00Z",
            "closedDate": "2025-01-18T15:00:00Z",
            "assignee": { "_id": "asg1", "fullname": "Jane Smith", "email": "jane@example.com", "username": "jsmith" },
            "updated": "2025-01-16T08:00:00Z"
        }
        """;

        var ticket = Deserialize<Ticket>(json);

        ticket.Id.Should().Be("ticket001");
        ticket.Subject.Should().Be("Test Subject");
        ticket.Issue.Should().Be("<p>Test Issue</p>");
        ticket.Uid.Should().Be(1001);
        ticket.Deleted.Should().BeFalse();
        ticket.Version.Should().Be(3);
        ticket.Date.Should().Be(new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc));
        ticket.Updated.Should().Be(new DateTime(2025, 1, 16, 8, 0, 0, DateTimeKind.Utc));
        ticket.DueDate.Should().Be(new DateTime(2025, 1, 20, 0, 0, 0, DateTimeKind.Utc));
        ticket.ClosedDate.Should().Be(new DateTime(2025, 1, 18, 15, 0, 0, DateTimeKind.Utc));

        ticket.Group.Should().NotBeNull();
        ticket.Group!.Id.Should().Be("grp1");
        ticket.Group.Name.Should().Be("Support");

        ticket.Type.Should().NotBeNull();
        ticket.Type!.Id.Should().Be("type1");

        ticket.Priority.Should().NotBeNull();
        ticket.Priority!.Id.Should().Be("pri1");
        ticket.Priority.Name.Should().Be("Normal");

        ticket.Status.Should().NotBeNull();
        ticket.Status!.Id.Should().Be("s1");
        ticket.Status.Name.Should().Be("Open");

        ticket.Owner.Should().NotBeNull();
        ticket.Owner!.Id.Should().Be("own1");
        ticket.Owner.Fullname.Should().Be("John Doe");

        ticket.Assignee.Should().NotBeNull();
        ticket.Assignee!.Id.Should().Be("asg1");
        ticket.Assignee.Fullname.Should().Be("Jane Smith");

        ticket.Tags.Should().HaveCount(1);
        ticket.Tags[0].Id.Should().Be("tag1");
        ticket.Tags[0].Name.Should().Be("urgent");

        ticket.Comments.Should().HaveCount(1);
        ticket.Comments[0].Id.Should().Be("c1");
        ticket.Comments[0].Text.Should().Be("A comment");

        ticket.Notes.Should().HaveCount(1);
        ticket.Notes[0].Id.Should().Be("n1");
        ticket.Notes[0].Content.Should().Be("A note");

        ticket.Attachments.Should().HaveCount(1);
        ticket.Attachments[0].Id.Should().Be("att1");
        ticket.Attachments[0].MimeType.Should().Be("text/plain");

        ticket.History.Should().HaveCount(1);
        ticket.History[0].Id.Should().Be("h1");
        ticket.History[0].Action.Should().Be("ticket:created");

        ticket.Subscribers.Should().BeEquivalentTo(["user1", "user2"]);
    }

    [Fact]
    public void Ticket_Deserialize_MinimalJson_DefaultsCorrectly()
    {
        var json = """
        {
            "_id": "ticket002",
            "uid": 42,
            "date": "2025-06-01T00:00:00Z",
            "updated": "2025-06-01T00:00:00Z"
        }
        """;

        var ticket = Deserialize<Ticket>(json);

        ticket.Id.Should().Be("ticket002");
        ticket.Uid.Should().Be(42);
        ticket.Subject.Should().BeNull();
        ticket.Issue.Should().BeNull();
        ticket.Group.Should().BeNull();
        ticket.Type.Should().BeNull();
        ticket.Priority.Should().BeNull();
        ticket.Status.Should().BeNull();
        ticket.Owner.Should().BeNull();
        ticket.Assignee.Should().BeNull();
        ticket.ClosedDate.Should().BeNull();
        ticket.Tags.Should().BeEmpty();
        ticket.Comments.Should().BeEmpty();
        ticket.Notes.Should().BeEmpty();
        ticket.Attachments.Should().BeEmpty();
        ticket.History.Should().BeEmpty();
        ticket.Subscribers.Should().BeEmpty();
        ticket.Deleted.Should().BeFalse();
        ticket.Version.Should().Be(0);
    }

    [Fact]
    public void Ticket_Deserialize_NullNestedObjects_HandledGracefully()
    {
        var json = """
        {
            "_id": "ticket003",
            "uid": 1,
            "date": "2025-01-01T00:00:00Z",
            "updated": "2025-01-01T00:00:00Z",
            "group": null,
            "type": null,
            "priority": null,
            "status": null,
            "owner": null,
            "assignee": null,
            "closedDate": null
        }
        """;

        var ticket = Deserialize<Ticket>(json);

        ticket.Group.Should().BeNull();
        ticket.Type.Should().BeNull();
        ticket.Priority.Should().BeNull();
        ticket.Status.Should().BeNull();
        ticket.Owner.Should().BeNull();
        ticket.Assignee.Should().BeNull();
        ticket.ClosedDate.Should().BeNull();
    }

    [Fact]
    public void Ticket_RoundTrip_PreservesData()
    {
        var original = new Ticket
        {
            Id = "rt1",
            Uid = 99,
            Subject = "Round trip",
            Issue = "Test body",
            Deleted = true,
            Date = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            Updated = new DateTime(2025, 3, 2, 0, 0, 0, DateTimeKind.Utc)
        };

        var json = JsonSerializer.Serialize(original, Options);
        var deserialized = Deserialize<Ticket>(json);

        deserialized.Id.Should().Be(original.Id);
        deserialized.Uid.Should().Be(original.Uid);
        deserialized.Subject.Should().Be(original.Subject);
        deserialized.Issue.Should().Be(original.Issue);
        deserialized.Deleted.Should().Be(original.Deleted);
    }

    #endregion

    #region Status

    [Fact]
    public void Status_Deserialize_FullJson_MapsAllProperties()
    {
        var json = """
        {
            "_id": "status1",
            "name": "Open",
            "htmlColor": "#29b955",
            "uid": 0,
            "order": 1,
            "slatimer": true,
            "isResolved": false,
            "isLocked": false,
            "__v": 2,
            "id2": "status1-alt"
        }
        """;

        var status = Deserialize<Status>(json);

        status.Id.Should().Be("status1");
        status.Name.Should().Be("Open");
        status.HtmlColor.Should().Be("#29b955");
        status.Uid.Should().Be(0);
        status.Order.Should().Be(1);
        status.Slatimer.Should().BeTrue();
        status.IsResolved.Should().BeFalse();
        status.IsLocked.Should().BeFalse();
        status.Version.Should().Be(2);
        status.Id2.Should().Be("status1-alt");
    }

    [Fact]
    public void Status_Deserialize_MinimalJson_DefaultsCorrectly()
    {
        var json = """{ "_id": "status2" }""";

        var status = Deserialize<Status>(json);

        status.Id.Should().Be("status2");
        status.Name.Should().BeNull();
        status.HtmlColor.Should().BeNull();
        status.Uid.Should().Be(0);
        status.Order.Should().Be(0);
        status.Slatimer.Should().BeFalse();
        status.IsResolved.Should().BeFalse();
        status.IsLocked.Should().BeFalse();
        status.Version.Should().Be(0);
        status.Id2.Should().BeNull();
    }

    [Fact]
    public void Status_RoundTrip_PreservesData()
    {
        var original = new Status
        {
            Id = "s-rt",
            Name = "Closed",
            HtmlColor = "#ccc",
            IsResolved = true,
            Order = 5,
            Slatimer = false
        };

        var json = JsonSerializer.Serialize(original, Options);
        var deserialized = Deserialize<Status>(json);

        deserialized.Id.Should().Be(original.Id);
        deserialized.Name.Should().Be(original.Name);
        deserialized.HtmlColor.Should().Be(original.HtmlColor);
        deserialized.IsResolved.Should().Be(original.IsResolved);
        deserialized.Order.Should().Be(original.Order);
    }

    #endregion

    #region Priority

    [Fact]
    public void Priority_Deserialize_FullJson_MapsAllProperties()
    {
        var json = """
        {
            "_id": "pri1",
            "name": "Critical",
            "overdueIn": 2,
            "htmlColor": "#e74c3c",
            "migrationNum": 3,
            "default": true,
            "__v": 1,
            "durationFormatted": "2 hours",
            "id2": "pri1-alt"
        }
        """;

        var priority = Deserialize<Priority>(json);

        priority.Id.Should().Be("pri1");
        priority.Name.Should().Be("Critical");
        priority.OverdueIn.Should().Be(2);
        priority.HtmlColor.Should().Be("#e74c3c");
        priority.MigrationNum.Should().Be(3);
        priority.Default.Should().BeTrue();
        priority.Version.Should().Be(1);
        priority.DurationFormatted.Should().Be("2 hours");
        priority.Id2.Should().Be("pri1-alt");
    }

    [Fact]
    public void Priority_Deserialize_MinimalJson_DefaultsCorrectly()
    {
        var json = """{ "_id": "pri2" }""";

        var priority = Deserialize<Priority>(json);

        priority.Id.Should().Be("pri2");
        priority.Name.Should().BeNull();
        priority.HtmlColor.Should().BeNull();
        priority.OverdueIn.Should().Be(0);
        priority.MigrationNum.Should().Be(0);
        priority.Default.Should().BeFalse();
        priority.Version.Should().Be(0);
        priority.DurationFormatted.Should().BeNull();
        priority.Id2.Should().BeNull();
    }

    [Fact]
    public void Priority_RoundTrip_PreservesData()
    {
        var original = new Priority
        {
            Id = "p-rt",
            Name = "High",
            OverdueIn = 24,
            HtmlColor = "#ff0000",
            Default = true
        };

        var json = JsonSerializer.Serialize(original, Options);
        var deserialized = Deserialize<Priority>(json);

        deserialized.Id.Should().Be(original.Id);
        deserialized.Name.Should().Be(original.Name);
        deserialized.OverdueIn.Should().Be(original.OverdueIn);
        deserialized.HtmlColor.Should().Be(original.HtmlColor);
        deserialized.Default.Should().Be(original.Default);
    }

    #endregion

    #region Group

    [Fact]
    public void Group_Deserialize_FullJson_MapsAllProperties()
    {
        var json = """
        {
            "_id": "grp1",
            "name": "IT Support",
            "members": [
                { "_id": "m1", "fullname": "Alice", "username": "alice", "email": "alice@test.com" },
                { "_id": "m2", "fullname": "Bob", "username": "bob", "email": "bob@test.com" }
            ],
            "sendMailTo": ["admin@test.com"],
            "public": true,
            "__v": 4
        }
        """;

        var group = Deserialize<Group>(json);

        group.Id.Should().Be("grp1");
        group.Name.Should().Be("IT Support");
        group.Members.Should().HaveCount(2);
        group.Members[0].Id.Should().Be("m1");
        group.Members[0].Fullname.Should().Be("Alice");
        group.Members[1].Id.Should().Be("m2");
        group.SendMailTo.Should().BeEquivalentTo(["admin@test.com"]);
        group.Public.Should().BeTrue();
        group.Version.Should().Be(4);
    }

    [Fact]
    public void Group_Deserialize_MinimalJson_DefaultsCorrectly()
    {
        var json = """{ "_id": "grp2" }""";

        var group = Deserialize<Group>(json);

        group.Id.Should().Be("grp2");
        group.Name.Should().BeNull();
        group.Members.Should().BeEmpty();
        group.SendMailTo.Should().BeEmpty();
        group.Public.Should().BeFalse();
        group.Version.Should().Be(0);
    }

    [Fact]
    public void Group_Deserialize_EmptyMembers_ReturnsEmptyList()
    {
        var json = """
        {
            "_id": "grp3",
            "name": "Empty Group",
            "members": [],
            "sendMailTo": []
        }
        """;

        var group = Deserialize<Group>(json);

        group.Members.Should().BeEmpty();
        group.SendMailTo.Should().BeEmpty();
    }

    [Fact]
    public void Group_RoundTrip_PreservesData()
    {
        var original = new Group
        {
            Id = "g-rt",
            Name = "Test Group",
            Public = true,
            Version = 2,
            Members = [new Assignee { Id = "a1", Fullname = "Test User" }]
        };

        var json = JsonSerializer.Serialize(original, Options);
        var deserialized = Deserialize<Group>(json);

        deserialized.Id.Should().Be(original.Id);
        deserialized.Name.Should().Be(original.Name);
        deserialized.Public.Should().Be(original.Public);
        deserialized.Version.Should().Be(original.Version);
        deserialized.Members.Should().HaveCount(1);
        deserialized.Members[0].Id.Should().Be("a1");
    }

    #endregion

    #region Owner

    [Fact]
    public void Owner_Deserialize_FullJson_MapsAllProperties()
    {
        var json = """
        {
            "_id": "own1",
            "username": "jdoe",
            "fullname": "John Doe",
            "email": "john@example.com",
            "role": "admin",
            "title": "Senior Engineer"
        }
        """;

        var owner = Deserialize<Owner>(json);

        owner.Id.Should().Be("own1");
        owner.Username.Should().Be("jdoe");
        owner.Fullname.Should().Be("John Doe");
        owner.Email.Should().Be("john@example.com");
        owner.Role.Should().Be("admin");
        owner.Title.Should().Be("Senior Engineer");
    }

    [Fact]
    public void Owner_Deserialize_MinimalJson_DefaultsCorrectly()
    {
        var json = """{ "_id": "own2" }""";

        var owner = Deserialize<Owner>(json);

        owner.Id.Should().Be("own2");
        owner.Username.Should().BeNull();
        owner.Fullname.Should().BeNull();
        owner.Email.Should().BeNull();
        owner.Role.Should().BeNull();
        owner.Title.Should().BeNull();
    }

    [Fact]
    public void Owner_Deserialize_NullOptionalFields_HandledGracefully()
    {
        var json = """
        {
            "_id": "own3",
            "username": null,
            "fullname": null,
            "email": null,
            "role": null,
            "title": null
        }
        """;

        var owner = Deserialize<Owner>(json);

        owner.Id.Should().Be("own3");
        owner.Username.Should().BeNull();
        owner.Fullname.Should().BeNull();
        owner.Email.Should().BeNull();
        owner.Role.Should().BeNull();
        owner.Title.Should().BeNull();
    }

    [Fact]
    public void Owner_RoundTrip_PreservesData()
    {
        var original = new Owner
        {
            Id = "o-rt",
            Username = "testuser",
            Fullname = "Test User",
            Email = "test@test.com",
            Role = "agent",
            Title = "Technician"
        };

        var json = JsonSerializer.Serialize(original, Options);
        var deserialized = Deserialize<Owner>(json);

        deserialized.Id.Should().Be(original.Id);
        deserialized.Username.Should().Be(original.Username);
        deserialized.Fullname.Should().Be(original.Fullname);
        deserialized.Email.Should().Be(original.Email);
        deserialized.Role.Should().Be(original.Role);
        deserialized.Title.Should().Be(original.Title);
    }

    #endregion

    #region Assignee

    [Fact]
    public void Assignee_Deserialize_FullJson_MapsAllProperties()
    {
        var json = """
        {
            "_id": "asg1",
            "username": "jsmith",
            "fullname": "Jane Smith",
            "email": "jane@example.com",
            "title": "Support Lead",
            "deleted": false
        }
        """;

        var assignee = Deserialize<Assignee>(json);

        assignee.Id.Should().Be("asg1");
        assignee.Username.Should().Be("jsmith");
        assignee.Fullname.Should().Be("Jane Smith");
        assignee.Email.Should().Be("jane@example.com");
        assignee.Title.Should().Be("Support Lead");
        assignee.Deleted.Should().BeFalse();
    }

    [Fact]
    public void Assignee_Deserialize_RoleAsString_ExtractsRoleName()
    {
        var json = """
        {
            "_id": "asg2",
            "fullname": "Test",
            "role": "admin"
        }
        """;

        var assignee = Deserialize<Assignee>(json);

        assignee.Id.Should().Be("asg2");
        assignee.RoleName.Should().Be("admin");
    }

    [Fact]
    public void Assignee_Deserialize_RoleAsObject_ExtractsRoleName()
    {
        var json = """
        {
            "_id": "asg3",
            "fullname": "Test",
            "role": { "_id": "role1", "name": "Agent" }
        }
        """;

        var assignee = Deserialize<Assignee>(json);

        assignee.Id.Should().Be("asg3");
        assignee.RoleName.Should().Be("Agent");
    }

    [Fact]
    public void Assignee_Deserialize_RoleNull_RoleNameIsNull()
    {
        var json = """
        {
            "_id": "asg4",
            "fullname": "Test",
            "role": null
        }
        """;

        var assignee = Deserialize<Assignee>(json);

        assignee.RoleName.Should().BeNull();
    }

    [Fact]
    public void Assignee_Deserialize_MinimalJson_DefaultsCorrectly()
    {
        var json = """{ "_id": "asg5" }""";

        var assignee = Deserialize<Assignee>(json);

        assignee.Id.Should().Be("asg5");
        assignee.Username.Should().BeNull();
        assignee.Fullname.Should().BeNull();
        assignee.Email.Should().BeNull();
        assignee.Title.Should().BeNull();
        assignee.Deleted.Should().BeFalse();
        assignee.RoleName.Should().BeNull();
    }

    [Fact]
    public void Assignee_Deserialize_DeletedTrue_MapsCorrectly()
    {
        var json = """
        {
            "_id": "asg6",
            "fullname": "Deleted User",
            "deleted": true
        }
        """;

        var assignee = Deserialize<Assignee>(json);

        assignee.Deleted.Should().BeTrue();
    }

    #endregion

    #region Attachment

    [Fact]
    public void Attachment_Deserialize_FullJson_MapsAllProperties()
    {
        var json = """
        {
            "_id": "att1",
            "name": "screenshot.png",
            "path": "/uploads/tickets/att1/screenshot.png",
            "type": "image/png",
            "size": 204800,
            "uploadDate": "2025-02-10T14:30:00Z"
        }
        """;

        var attachment = Deserialize<Attachment>(json);

        attachment.Id.Should().Be("att1");
        attachment.Name.Should().Be("screenshot.png");
        attachment.Path.Should().Be("/uploads/tickets/att1/screenshot.png");
        attachment.MimeType.Should().Be("image/png");
        attachment.Size.Should().Be(204800);
        attachment.UploadDate.Should().Be(new DateTime(2025, 2, 10, 14, 30, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Attachment_Deserialize_MinimalJson_DefaultsCorrectly()
    {
        var json = """{ "_id": "att2" }""";

        var attachment = Deserialize<Attachment>(json);

        attachment.Id.Should().Be("att2");
        attachment.Name.Should().BeNull();
        attachment.Path.Should().BeNull();
        attachment.MimeType.Should().BeNull();
        attachment.Size.Should().Be(0);
        attachment.UploadDate.Should().BeNull();
    }

    [Fact]
    public void Attachment_Deserialize_TypeMapsToMimeType()
    {
        var json = """
        {
            "_id": "att3",
            "type": "application/pdf"
        }
        """;

        var attachment = Deserialize<Attachment>(json);

        attachment.MimeType.Should().Be("application/pdf");
    }

    [Fact]
    public void Attachment_IsImage_ComputedFromMimeType()
    {
        var json = """
        {
            "_id": "att4",
            "type": "image/jpeg",
            "size": 500
        }
        """;

        var attachment = Deserialize<Attachment>(json);

        attachment.IsImage.Should().BeTrue();
    }

    [Fact]
    public void Attachment_SizeFormatted_ComputedFromSize()
    {
        var json = """
        {
            "_id": "att5",
            "size": 2048
        }
        """;

        var attachment = Deserialize<Attachment>(json);

        attachment.SizeFormatted.Should().Be("2.0 KB");
    }

    [Fact]
    public void Attachment_RoundTrip_PreservesData()
    {
        var original = new Attachment
        {
            Id = "a-rt",
            Name = "doc.pdf",
            Path = "/uploads/doc.pdf",
            MimeType = "application/pdf",
            Size = 65536,
            UploadDate = new DateTime(2025, 5, 1, 12, 0, 0, DateTimeKind.Utc)
        };

        var json = JsonSerializer.Serialize(original, Options);
        var deserialized = Deserialize<Attachment>(json);

        deserialized.Id.Should().Be(original.Id);
        deserialized.Name.Should().Be(original.Name);
        deserialized.Path.Should().Be(original.Path);
        deserialized.MimeType.Should().Be(original.MimeType);
        deserialized.Size.Should().Be(original.Size);
        deserialized.UploadDate.Should().Be(original.UploadDate);
    }

    #endregion

    #region Comment

    [Fact]
    public void Comment_Deserialize_FullJson_MapsAllProperties()
    {
        var json = """
        {
            "_id": "com1",
            "date": "2025-03-10T09:00:00Z",
            "owner": { "_id": "own1", "fullname": "Commenter", "username": "commenter1" },
            "comment": "This is a comment with <b>HTML</b>",
            "deleted": false
        }
        """;

        var comment = Deserialize<Comment>(json);

        comment.Id.Should().Be("com1");
        comment.Date.Should().Be(new DateTime(2025, 3, 10, 9, 0, 0, DateTimeKind.Utc));
        comment.Owner.Should().NotBeNull();
        comment.Owner!.Id.Should().Be("own1");
        comment.Owner.Fullname.Should().Be("Commenter");
        comment.Text.Should().Be("This is a comment with <b>HTML</b>");
        comment.Deleted.Should().BeFalse();
    }

    [Fact]
    public void Comment_Deserialize_CommentFieldMapsToText()
    {
        var json = """
        {
            "_id": "com2",
            "comment": "Mapped to Text property",
            "date": "2025-01-01T00:00:00Z"
        }
        """;

        var comment = Deserialize<Comment>(json);

        comment.Text.Should().Be("Mapped to Text property");
    }

    [Fact]
    public void Comment_Deserialize_MinimalJson_DefaultsCorrectly()
    {
        var json = """
        {
            "_id": "com3",
            "date": "2025-01-01T00:00:00Z"
        }
        """;

        var comment = Deserialize<Comment>(json);

        comment.Id.Should().Be("com3");
        comment.Text.Should().BeNull();
        comment.Owner.Should().BeNull();
        comment.Deleted.Should().BeFalse();
    }

    [Fact]
    public void Comment_Deserialize_DeletedTrue_MapsCorrectly()
    {
        var json = """
        {
            "_id": "com4",
            "comment": "Deleted comment",
            "date": "2025-01-01T00:00:00Z",
            "deleted": true
        }
        """;

        var comment = Deserialize<Comment>(json);

        comment.Deleted.Should().BeTrue();
    }

    [Fact]
    public void Comment_RoundTrip_PreservesData()
    {
        var original = new Comment
        {
            Id = "c-rt",
            Text = "Round trip comment",
            Date = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            Deleted = false
        };

        var json = JsonSerializer.Serialize(original, Options);
        var deserialized = Deserialize<Comment>(json);

        deserialized.Id.Should().Be(original.Id);
        deserialized.Text.Should().Be(original.Text);
        deserialized.Deleted.Should().Be(original.Deleted);
    }

    #endregion

    #region Note

    [Fact]
    public void Note_Deserialize_FullJson_MapsAllProperties()
    {
        var json = """
        {
            "_id": "note1",
            "owner": { "_id": "own1", "fullname": "Note Author", "username": "author1" },
            "date": "2025-04-01T16:00:00Z",
            "note": "Internal note content"
        }
        """;

        var note = Deserialize<Note>(json);

        note.Id.Should().Be("note1");
        note.Owner.Should().NotBeNull();
        note.Owner!.Id.Should().Be("own1");
        note.Owner.Fullname.Should().Be("Note Author");
        note.Date.Should().Be(new DateTime(2025, 4, 1, 16, 0, 0, DateTimeKind.Utc));
        note.Content.Should().Be("Internal note content");
    }

    [Fact]
    public void Note_Deserialize_NoteFieldMapsToContent()
    {
        var json = """
        {
            "_id": "note2",
            "note": "This maps to Content",
            "date": "2025-01-01T00:00:00Z"
        }
        """;

        var note = Deserialize<Note>(json);

        note.Content.Should().Be("This maps to Content");
    }

    [Fact]
    public void Note_Deserialize_MinimalJson_DefaultsCorrectly()
    {
        var json = """
        {
            "_id": "note3",
            "date": "2025-01-01T00:00:00Z"
        }
        """;

        var note = Deserialize<Note>(json);

        note.Id.Should().Be("note3");
        note.Content.Should().BeNull();
        note.Owner.Should().BeNull();
    }

    [Fact]
    public void Note_RoundTrip_PreservesData()
    {
        var original = new Note
        {
            Id = "n-rt",
            Content = "Round trip note",
            Date = new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            Owner = new Owner { Id = "o1", Fullname = "Author" }
        };

        var json = JsonSerializer.Serialize(original, Options);
        var deserialized = Deserialize<Note>(json);

        deserialized.Id.Should().Be(original.Id);
        deserialized.Content.Should().Be(original.Content);
        deserialized.Owner.Should().NotBeNull();
        deserialized.Owner!.Id.Should().Be("o1");
    }

    #endregion

    #region HistoryItem

    [Fact]
    public void HistoryItem_Deserialize_FullJson_MapsAllProperties()
    {
        var json = """
        {
            "_id": "hist1",
            "action": "ticket:created",
            "description": "Ticket was created",
            "date": "2025-05-01T08:00:00Z",
            "owner": { "_id": "own1", "fullname": "Creator", "username": "creator1" }
        }
        """;

        var history = Deserialize<HistoryItem>(json);

        history.Id.Should().Be("hist1");
        history.Action.Should().Be("ticket:created");
        history.Description.Should().Be("Ticket was created");
        history.Date.Should().Be(new DateTime(2025, 5, 1, 8, 0, 0, DateTimeKind.Utc));
        history.Owner.Should().NotBeNull();
        history.Owner!.Id.Should().Be("own1");
        history.Owner.Fullname.Should().Be("Creator");
    }

    [Fact]
    public void HistoryItem_Deserialize_MinimalJson_DefaultsCorrectly()
    {
        var json = """
        {
            "_id": "hist2",
            "date": "2025-01-01T00:00:00Z"
        }
        """;

        var history = Deserialize<HistoryItem>(json);

        history.Id.Should().Be("hist2");
        history.Action.Should().BeNull();
        history.Description.Should().BeNull();
        history.Owner.Should().BeNull();
    }

    [Fact]
    public void HistoryItem_Deserialize_VariousActions()
    {
        var actions = new[] { "ticket:created", "ticket:updated", "ticket:comment:added", "ticket:note:added" };

        foreach (var action in actions)
        {
            var json = $$"""
            {
                "_id": "hist-action",
                "action": "{{action}}",
                "date": "2025-01-01T00:00:00Z"
            }
            """;

            var history = Deserialize<HistoryItem>(json);
            history.Action.Should().Be(action);
        }
    }

    [Fact]
    public void HistoryItem_RoundTrip_PreservesData()
    {
        var original = new HistoryItem
        {
            Id = "h-rt",
            Action = "ticket:updated",
            Description = "Status changed",
            Date = new DateTime(2025, 8, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var json = JsonSerializer.Serialize(original, Options);
        var deserialized = Deserialize<HistoryItem>(json);

        deserialized.Id.Should().Be(original.Id);
        deserialized.Action.Should().Be(original.Action);
        deserialized.Description.Should().Be(original.Description);
    }

    #endregion

    #region TicketType

    [Fact]
    public void TicketType_Deserialize_FullJson_MapsAllProperties()
    {
        var json = """
        {
            "_id": "type1",
            "name": "Issue",
            "priorities": [
                { "_id": "pri1", "name": "Normal", "htmlColor": "#ccc", "overdueIn": 48 },
                { "_id": "pri2", "name": "Critical", "htmlColor": "#f00", "overdueIn": 2 }
            ],
            "__v": 1
        }
        """;

        var ticketType = Deserialize<TicketType>(json);

        ticketType.Id.Should().Be("type1");
        ticketType.Name.Should().Be("Issue");
        ticketType.Priorities.Should().HaveCount(2);
        ticketType.Priorities[0].Id.Should().Be("pri1");
        ticketType.Priorities[0].Name.Should().Be("Normal");
        ticketType.Priorities[1].Id.Should().Be("pri2");
        ticketType.Priorities[1].Name.Should().Be("Critical");
        ticketType.Version.Should().Be(1);
    }

    [Fact]
    public void TicketType_Deserialize_MinimalJson_DefaultsCorrectly()
    {
        var json = """{ "_id": "type2" }""";

        var ticketType = Deserialize<TicketType>(json);

        ticketType.Id.Should().Be("type2");
        ticketType.Name.Should().BeNull();
        ticketType.Priorities.Should().BeEmpty();
        ticketType.Version.Should().Be(0);
    }

    [Fact]
    public void TicketType_Deserialize_EmptyPriorities_ReturnsEmptyList()
    {
        var json = """
        {
            "_id": "type3",
            "name": "Task",
            "priorities": []
        }
        """;

        var ticketType = Deserialize<TicketType>(json);

        ticketType.Priorities.Should().BeEmpty();
    }

    [Fact]
    public void TicketType_RoundTrip_PreservesData()
    {
        var original = new TicketType
        {
            Id = "t-rt",
            Name = "Bug",
            Version = 3,
            Priorities = [new Priority { Id = "p1", Name = "High" }]
        };

        var json = JsonSerializer.Serialize(original, Options);
        var deserialized = Deserialize<TicketType>(json);

        deserialized.Id.Should().Be(original.Id);
        deserialized.Name.Should().Be(original.Name);
        deserialized.Version.Should().Be(original.Version);
        deserialized.Priorities.Should().HaveCount(1);
        deserialized.Priorities[0].Id.Should().Be("p1");
    }

    #endregion

    #region Tag

    [Fact]
    public void Tag_Deserialize_FullJson_MapsAllProperties()
    {
        var json = """
        {
            "_id": "tag1",
            "name": "Urgent",
            "normalized": "urgent",
            "__v": 0
        }
        """;

        var tag = Deserialize<Tag>(json);

        tag.Id.Should().Be("tag1");
        tag.Name.Should().Be("Urgent");
        tag.Normalized.Should().Be("urgent");
        tag.Version.Should().Be(0);
    }

    [Fact]
    public void Tag_Deserialize_MinimalJson_DefaultsCorrectly()
    {
        var json = """{ "_id": "tag2" }""";

        var tag = Deserialize<Tag>(json);

        tag.Id.Should().Be("tag2");
        tag.Name.Should().BeNull();
        tag.Normalized.Should().BeNull();
        tag.Version.Should().Be(0);
    }

    [Fact]
    public void Tag_RoundTrip_PreservesData()
    {
        var original = new Tag
        {
            Id = "t-rt",
            Name = "Network",
            Normalized = "network",
            Version = 1
        };

        var json = JsonSerializer.Serialize(original, Options);
        var deserialized = Deserialize<Tag>(json);

        deserialized.Id.Should().Be(original.Id);
        deserialized.Name.Should().Be(original.Name);
        deserialized.Normalized.Should().Be(original.Normalized);
        deserialized.Version.Should().Be(original.Version);
    }

    #endregion

    #region Role

    [Fact]
    public void Role_Deserialize_FullJson_MapsAllProperties()
    {
        var json = """
        {
            "_id": "role1",
            "name": "Admin",
            "description": "Administrator role",
            "normalized": "admin",
            "isAdmin": true,
            "isAgent": true
        }
        """;

        var role = Deserialize<Role>(json);

        role.Id.Should().Be("role1");
        role.Name.Should().Be("Admin");
        role.Description.Should().Be("Administrator role");
        role.Normalized.Should().Be("admin");
        role.IsAdmin.Should().BeTrue();
        role.IsAgent.Should().BeTrue();
    }

    [Fact]
    public void Role_Deserialize_MinimalJson_DefaultsCorrectly()
    {
        var json = """{ "_id": "role2" }""";

        var role = Deserialize<Role>(json);

        role.Id.Should().Be("role2");
        role.Name.Should().BeNull();
        role.Description.Should().BeNull();
        role.Normalized.Should().BeNull();
        role.IsAdmin.Should().BeFalse();
        role.IsAgent.Should().BeFalse();
    }

    [Fact]
    public void Role_Deserialize_AgentNotAdmin()
    {
        var json = """
        {
            "_id": "role3",
            "name": "Agent",
            "isAdmin": false,
            "isAgent": true
        }
        """;

        var role = Deserialize<Role>(json);

        role.IsAdmin.Should().BeFalse();
        role.IsAgent.Should().BeTrue();
    }

    [Fact]
    public void Role_RoundTrip_PreservesData()
    {
        var original = new Role
        {
            Id = "r-rt",
            Name = "Support",
            Description = "Support agent",
            Normalized = "support",
            IsAdmin = false,
            IsAgent = true
        };

        var json = JsonSerializer.Serialize(original, Options);
        var deserialized = Deserialize<Role>(json);

        deserialized.Id.Should().Be(original.Id);
        deserialized.Name.Should().Be(original.Name);
        deserialized.Description.Should().Be(original.Description);
        deserialized.IsAdmin.Should().Be(original.IsAdmin);
        deserialized.IsAgent.Should().Be(original.IsAgent);
    }

    #endregion

    #region User

    [Fact]
    public void User_Deserialize_FullJson_MapsAllProperties()
    {
        var json = """
        {
            "_id": "user-internal-1",
            "username": "jdoe",
            "fullname": "John Doe",
            "email": "john@example.com",
            "role": { "_id": "role1", "name": "Admin", "isAdmin": true, "isAgent": true },
            "title": "IT Manager",
            "hasL2Auth": true,
            "deleted": false,
            "lastOnline": "2025-06-15T12:00:00Z",
            "id": "user-public-1",
            "groups": [
                { "_id": "grp1", "name": "IT Support" }
            ]
        }
        """;

        var user = Deserialize<User>(json);

        user.InternalId.Should().Be("user-internal-1");
        user.Username.Should().Be("jdoe");
        user.Fullname.Should().Be("John Doe");
        user.Email.Should().Be("john@example.com");
        user.Role.Should().NotBeNull();
        user.Role!.Id.Should().Be("role1");
        user.Role.Name.Should().Be("Admin");
        user.Role.IsAdmin.Should().BeTrue();
        user.Title.Should().Be("IT Manager");
        user.HasL2Auth.Should().BeTrue();
        user.Deleted.Should().BeFalse();
        user.LastOnline.Should().Be(new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc));
        user.Id.Should().Be("user-public-1");
        user.Groups.Should().HaveCount(1);
        user.Groups[0].Id.Should().Be("grp1");
        user.Groups[0].Name.Should().Be("IT Support");
    }

    [Fact]
    public void User_Deserialize_IdMapsToInternalId()
    {
        var json = """
        {
            "_id": "mongo-object-id-123",
            "username": "test"
        }
        """;

        var user = Deserialize<User>(json);

        user.InternalId.Should().Be("mongo-object-id-123");
    }

    [Fact]
    public void User_Deserialize_MinimalJson_DefaultsCorrectly()
    {
        var json = """{ "_id": "user2" }""";

        var user = Deserialize<User>(json);

        user.InternalId.Should().Be("user2");
        user.Username.Should().BeNull();
        user.Fullname.Should().BeNull();
        user.Email.Should().BeNull();
        user.Role.Should().BeNull();
        user.Title.Should().BeNull();
        user.HasL2Auth.Should().BeFalse();
        user.Deleted.Should().BeFalse();
        user.Groups.Should().BeEmpty();
    }

    [Fact]
    public void User_Deserialize_NullRole_HandledGracefully()
    {
        var json = """
        {
            "_id": "user3",
            "username": "noRole",
            "role": null
        }
        """;

        var user = Deserialize<User>(json);

        user.Role.Should().BeNull();
    }

    [Fact]
    public void User_Deserialize_EmptyGroups_ReturnsEmptyList()
    {
        var json = """
        {
            "_id": "user4",
            "groups": []
        }
        """;

        var user = Deserialize<User>(json);

        user.Groups.Should().BeEmpty();
    }

    [Fact]
    public void User_RoundTrip_PreservesData()
    {
        var original = new User
        {
            InternalId = "u-rt",
            Username = "roundtrip",
            Fullname = "Round Trip User",
            Email = "rt@test.com",
            Title = "Tester",
            HasL2Auth = true,
            Deleted = false,
            Id = "u-rt-public"
        };

        var json = JsonSerializer.Serialize(original, Options);
        var deserialized = Deserialize<User>(json);

        deserialized.InternalId.Should().Be(original.InternalId);
        deserialized.Username.Should().Be(original.Username);
        deserialized.Fullname.Should().Be(original.Fullname);
        deserialized.Email.Should().Be(original.Email);
        deserialized.Title.Should().Be(original.Title);
        deserialized.HasL2Auth.Should().Be(original.HasL2Auth);
        deserialized.Id.Should().Be(original.Id);
    }

    #endregion

    #region Response Wrappers

    [Fact]
    public void GetUserResponse_Deserialize_FullJson_MapsAllProperties()
    {
        var json = """
        {
            "success": true,
            "count": 2,
            "users": [
                { "_id": "u1", "username": "user1", "fullname": "User One" },
                { "_id": "u2", "username": "user2", "fullname": "User Two" }
            ]
        }
        """;

        var response = Deserialize<GetUserResponse>(json);

        response.Success.Should().BeTrue();
        response.Count.Should().Be(2);
        response.Users.Should().HaveCount(2);
        response.Users[0].InternalId.Should().Be("u1");
        response.Users[0].Username.Should().Be("user1");
        response.Users[1].InternalId.Should().Be("u2");
        response.Users[1].Fullname.Should().Be("User Two");
    }

    [Fact]
    public void GetUserResponse_Deserialize_EmptyUsers_ReturnsEmptyList()
    {
        var json = """
        {
            "success": true,
            "count": 0,
            "users": []
        }
        """;

        var response = Deserialize<GetUserResponse>(json);

        response.Success.Should().BeTrue();
        response.Count.Should().Be(0);
        response.Users.Should().BeEmpty();
    }

    [Fact]
    public void GetUserResponse_Deserialize_MinimalJson_DefaultsCorrectly()
    {
        var json = """{}""";

        var response = Deserialize<GetUserResponse>(json);

        response.Success.Should().BeFalse();
        response.Count.Should().Be(0);
        response.Users.Should().BeEmpty();
    }

    [Fact]
    public void GetGroupResponse_Deserialize_FullJson_MapsAllProperties()
    {
        var json = """
        {
            "success": true,
            "groups": [
                {
                    "_id": "grp1",
                    "name": "IT Support",
                    "members": [{ "_id": "m1", "fullname": "Alice" }],
                    "public": true,
                    "__v": 1
                },
                {
                    "_id": "grp2",
                    "name": "HR",
                    "members": [],
                    "public": false,
                    "__v": 0
                }
            ]
        }
        """;

        var response = Deserialize<GetGroupResponse>(json);

        response.Success.Should().BeTrue();
        response.Groups.Should().HaveCount(2);
        response.Groups[0].Id.Should().Be("grp1");
        response.Groups[0].Name.Should().Be("IT Support");
        response.Groups[0].Members.Should().HaveCount(1);
        response.Groups[0].Public.Should().BeTrue();
        response.Groups[1].Id.Should().Be("grp2");
        response.Groups[1].Public.Should().BeFalse();
    }

    [Fact]
    public void GetGroupResponse_Deserialize_EmptyGroups_ReturnsEmptyList()
    {
        var json = """
        {
            "success": true,
            "groups": []
        }
        """;

        var response = Deserialize<GetGroupResponse>(json);

        response.Success.Should().BeTrue();
        response.Groups.Should().BeEmpty();
    }

    [Fact]
    public void GetGroupResponse_Deserialize_MinimalJson_DefaultsCorrectly()
    {
        var json = """{}""";

        var response = Deserialize<GetGroupResponse>(json);

        response.Success.Should().BeFalse();
        response.Groups.Should().BeEmpty();
    }

    [Fact]
    public void GetStatusResponse_Deserialize_FullJson_MapsAllProperties()
    {
        var json = """
        {
            "success": true,
            "status": [
                { "_id": "s1", "name": "New", "htmlColor": "#29b955", "uid": 0, "order": 0 },
                { "_id": "s2", "name": "Open", "htmlColor": "#e74c3c", "uid": 1, "order": 1 },
                { "_id": "s3", "name": "Closed", "htmlColor": "#ccc", "isResolved": true, "order": 3 }
            ]
        }
        """;

        var response = Deserialize<GetStatusResponse>(json);

        response.Success.Should().BeTrue();
        response.Statuses.Should().HaveCount(3);
        response.Statuses[0].Id.Should().Be("s1");
        response.Statuses[0].Name.Should().Be("New");
        response.Statuses[1].Id.Should().Be("s2");
        response.Statuses[1].Name.Should().Be("Open");
        response.Statuses[2].Id.Should().Be("s3");
        response.Statuses[2].IsResolved.Should().BeTrue();
    }

    [Fact]
    public void GetStatusResponse_Deserialize_StatusFieldMapsToStatuses()
    {
        // The JSON field is "status" but the C# property is "Statuses"
        var json = """
        {
            "success": true,
            "status": [
                { "_id": "s1", "name": "Open" }
            ]
        }
        """;

        var response = Deserialize<GetStatusResponse>(json);

        response.Statuses.Should().HaveCount(1);
        response.Statuses[0].Name.Should().Be("Open");
    }

    [Fact]
    public void GetStatusResponse_Deserialize_EmptyStatuses_ReturnsEmptyList()
    {
        var json = """
        {
            "success": true,
            "status": []
        }
        """;

        var response = Deserialize<GetStatusResponse>(json);

        response.Statuses.Should().BeEmpty();
    }

    [Fact]
    public void GetStatusResponse_Deserialize_MinimalJson_DefaultsCorrectly()
    {
        var json = """{}""";

        var response = Deserialize<GetStatusResponse>(json);

        response.Success.Should().BeFalse();
        response.Statuses.Should().BeEmpty();
    }

    [Fact]
    public void GetTicketsResponse_Deserialize_FullJson_MapsAllProperties()
    {
        var json = """
        {
            "success": true,
            "count": 2,
            "tickets": [
                {
                    "_id": "t1",
                    "uid": 1001,
                    "subject": "First ticket",
                    "date": "2025-01-01T00:00:00Z",
                    "updated": "2025-01-02T00:00:00Z",
                    "status": { "_id": "s1", "name": "Open" }
                },
                {
                    "_id": "t2",
                    "uid": 1002,
                    "subject": "Second ticket",
                    "date": "2025-01-03T00:00:00Z",
                    "updated": "2025-01-04T00:00:00Z",
                    "status": { "_id": "s2", "name": "Closed", "isResolved": true }
                }
            ]
        }
        """;

        var response = Deserialize<GetTicketsResponse>(json);

        response.Success.Should().BeTrue();
        response.Count.Should().Be(2);
        response.Tickets.Should().HaveCount(2);
        response.Tickets[0].Id.Should().Be("t1");
        response.Tickets[0].Uid.Should().Be(1001);
        response.Tickets[0].Subject.Should().Be("First ticket");
        response.Tickets[0].Status.Should().NotBeNull();
        response.Tickets[0].Status!.Name.Should().Be("Open");
        response.Tickets[1].Id.Should().Be("t2");
        response.Tickets[1].Uid.Should().Be(1002);
        response.Tickets[1].Status!.IsResolved.Should().BeTrue();
    }

    [Fact]
    public void GetTicketsResponse_Deserialize_EmptyTickets_ReturnsEmptyList()
    {
        var json = """
        {
            "success": true,
            "count": 0,
            "tickets": []
        }
        """;

        var response = Deserialize<GetTicketsResponse>(json);

        response.Success.Should().BeTrue();
        response.Count.Should().Be(0);
        response.Tickets.Should().BeEmpty();
    }

    [Fact]
    public void GetTicketsResponse_Deserialize_MinimalJson_DefaultsCorrectly()
    {
        var json = """{}""";

        var response = Deserialize<GetTicketsResponse>(json);

        response.Success.Should().BeFalse();
        response.Count.Should().Be(0);
        response.Tickets.Should().BeEmpty();
    }

    [Fact]
    public void GetTicketsResponse_Deserialize_NestedTicketWithFullData()
    {
        var json = """
        {
            "success": true,
            "count": 1,
            "tickets": [
                {
                    "_id": "t1",
                    "uid": 500,
                    "subject": "Full ticket in response",
                    "issue": "<p>Description</p>",
                    "date": "2025-03-01T00:00:00Z",
                    "updated": "2025-03-02T00:00:00Z",
                    "status": { "_id": "s1", "name": "Open", "htmlColor": "#29b955" },
                    "priority": { "_id": "p1", "name": "High", "htmlColor": "#e74c3c" },
                    "group": { "_id": "g1", "name": "Support" },
                    "owner": { "_id": "o1", "fullname": "Owner Name" },
                    "assignee": { "_id": "a1", "fullname": "Assignee Name" },
                    "type": { "_id": "tt1", "name": "Issue" },
                    "tags": [{ "_id": "tg1", "name": "network" }],
                    "comments": [{ "_id": "c1", "comment": "A comment", "date": "2025-03-01T12:00:00Z" }]
                }
            ]
        }
        """;

        var response = Deserialize<GetTicketsResponse>(json);

        var ticket = response.Tickets[0];
        ticket.Id.Should().Be("t1");
        ticket.Subject.Should().Be("Full ticket in response");
        ticket.Issue.Should().Be("<p>Description</p>");
        ticket.Status!.Name.Should().Be("Open");
        ticket.Priority!.Name.Should().Be("High");
        ticket.Group!.Name.Should().Be("Support");
        ticket.Owner!.Fullname.Should().Be("Owner Name");
        ticket.Assignee!.Fullname.Should().Be("Assignee Name");
        ticket.Type!.Name.Should().Be("Issue");
        ticket.Tags.Should().HaveCount(1);
        ticket.Comments.Should().HaveCount(1);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Deserialize_UnknownProperties_AreIgnored()
    {
        var json = """
        {
            "_id": "test1",
            "name": "Test",
            "unknownField": "should be ignored",
            "anotherUnknown": 42
        }
        """;

        var status = Deserialize<Status>(json);

        status.Id.Should().Be("test1");
        status.Name.Should().Be("Test");
    }

    [Fact]
    public void Deserialize_CaseInsensitive_MapsCorrectly()
    {
        var json = """
        {
            "_id": "ci1",
            "Name": "Upper case",
            "HtmlColor": "#fff",
            "IsResolved": true
        }
        """;

        var status = Deserialize<Status>(json);

        status.Id.Should().Be("ci1");
        status.Name.Should().Be("Upper case");
        status.HtmlColor.Should().Be("#fff");
        status.IsResolved.Should().BeTrue();
    }

    [Fact]
    public void Deserialize_EmptyJsonObject_ReturnsDefaults()
    {
        var json = """{}""";

        var ticket = Deserialize<Ticket>(json);

        ticket.Id.Should().Be(string.Empty);
        ticket.Uid.Should().Be(0);
        ticket.Subject.Should().BeNull();
        ticket.Tags.Should().BeEmpty();
        ticket.Comments.Should().BeEmpty();
    }

    [Fact]
    public void Ticket_Deserialize_MultipleComments_PreservesOrder()
    {
        var json = """
        {
            "_id": "t-order",
            "uid": 1,
            "date": "2025-01-01T00:00:00Z",
            "updated": "2025-01-01T00:00:00Z",
            "comments": [
                { "_id": "c1", "comment": "First", "date": "2025-01-01T10:00:00Z" },
                { "_id": "c2", "comment": "Second", "date": "2025-01-01T11:00:00Z" },
                { "_id": "c3", "comment": "Third", "date": "2025-01-01T12:00:00Z" }
            ]
        }
        """;

        var ticket = Deserialize<Ticket>(json);

        ticket.Comments.Should().HaveCount(3);
        ticket.Comments[0].Text.Should().Be("First");
        ticket.Comments[1].Text.Should().Be("Second");
        ticket.Comments[2].Text.Should().Be("Third");
    }

    [Fact]
    public void Ticket_Deserialize_MultipleTags_PreservesOrder()
    {
        var json = """
        {
            "_id": "t-tags",
            "uid": 1,
            "date": "2025-01-01T00:00:00Z",
            "updated": "2025-01-01T00:00:00Z",
            "tags": [
                { "_id": "tg1", "name": "alpha" },
                { "_id": "tg2", "name": "beta" },
                { "_id": "tg3", "name": "gamma" }
            ]
        }
        """;

        var ticket = Deserialize<Ticket>(json);

        ticket.Tags.Should().HaveCount(3);
        ticket.Tags[0].Name.Should().Be("alpha");
        ticket.Tags[1].Name.Should().Be("beta");
        ticket.Tags[2].Name.Should().Be("gamma");
    }

    [Fact]
    public void Ticket_Deserialize_MultipleAttachments_AllMapped()
    {
        var json = """
        {
            "_id": "t-att",
            "uid": 1,
            "date": "2025-01-01T00:00:00Z",
            "updated": "2025-01-01T00:00:00Z",
            "attachments": [
                { "_id": "a1", "name": "image.png", "type": "image/png", "size": 1024 },
                { "_id": "a2", "name": "doc.pdf", "type": "application/pdf", "size": 2048 }
            ]
        }
        """;

        var ticket = Deserialize<Ticket>(json);

        ticket.Attachments.Should().HaveCount(2);
        ticket.Attachments[0].Name.Should().Be("image.png");
        ticket.Attachments[0].IsImage.Should().BeTrue();
        ticket.Attachments[1].Name.Should().Be("doc.pdf");
        ticket.Attachments[1].IsImage.Should().BeFalse();
    }

    [Fact]
    public void Group_Deserialize_MultipleMembers_AllMapped()
    {
        var json = """
        {
            "_id": "grp-multi",
            "name": "Large Team",
            "members": [
                { "_id": "m1", "fullname": "Alice", "username": "alice" },
                { "_id": "m2", "fullname": "Bob", "username": "bob" },
                { "_id": "m3", "fullname": "Charlie", "username": "charlie" }
            ]
        }
        """;

        var group = Deserialize<Group>(json);

        group.Members.Should().HaveCount(3);
        group.Members[0].Fullname.Should().Be("Alice");
        group.Members[1].Fullname.Should().Be("Bob");
        group.Members[2].Fullname.Should().Be("Charlie");
    }

    #endregion
}
