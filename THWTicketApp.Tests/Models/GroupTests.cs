using System.Text.Json;
using FluentAssertions;
using THWTicketApp.Models;
using Xunit;

namespace THWTicketApp.Tests.Models;

public class GroupTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void Deserialize_JsonPropertyName_MapsIdCorrectly()
    {
        var json = """
        {
            "_id": "grp_abc123",
            "name": "IT Department",
            "public": true,
            "__v": 2
        }
        """;

        var group = JsonSerializer.Deserialize<Group>(json, JsonOptions);

        group.Should().NotBeNull();
        group!.Id.Should().Be("grp_abc123");
        group.Name.Should().Be("IT Department");
        group.Public.Should().BeTrue();
        group.Version.Should().Be(2);
    }

    [Fact]
    public void Deserialize_WithMembers_MapsCorrectly()
    {
        var json = """
        {
            "_id": "grp1",
            "name": "Support",
            "members": [
                { "_id": "m1", "username": "alice", "fullname": "Alice A" },
                { "_id": "m2", "username": "bob", "fullname": "Bob B" }
            ],
            "sendMailTo": ["admin@example.com", "team@example.com"],
            "public": false,
            "__v": 0
        }
        """;

        var group = JsonSerializer.Deserialize<Group>(json, JsonOptions);

        group.Should().NotBeNull();
        group!.Members.Should().HaveCount(2);
        group.Members[0].Id.Should().Be("m1");
        group.Members[1].Fullname.Should().Be("Bob B");
        group.SendMailTo.Should().HaveCount(2);
        group.SendMailTo.Should().Contain("admin@example.com");
    }

    [Fact]
    public void Deserialize_MinimalJson_DefaultsCorrectly()
    {
        var json = """{ "_id": "g1" }""";

        var group = JsonSerializer.Deserialize<Group>(json, JsonOptions);

        group.Should().NotBeNull();
        group!.Id.Should().Be("g1");
        group.Name.Should().BeNull();
        group.Members.Should().BeEmpty();
        group.SendMailTo.Should().BeEmpty();
        group.Public.Should().BeFalse();
        group.Version.Should().Be(0);
    }

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var group = new Group();

        group.Id.Should().BeNull();
        group.Name.Should().BeNull();
        group.Members.Should().NotBeNull().And.BeEmpty();
        group.SendMailTo.Should().NotBeNull().And.BeEmpty();
        group.Public.Should().BeFalse();
        group.Version.Should().Be(0);
    }
}
