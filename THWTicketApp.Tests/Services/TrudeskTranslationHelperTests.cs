using FluentAssertions;
using THWTicketApp.Services;
using Xunit;

namespace THWTicketApp.Tests.Services;

public class TrudeskTranslationHelperTests
{
    [Theory]
    [InlineData("Open", "Offen")]
    [InlineData("Pending", "Ausstehend")]
    [InlineData("Closed", "Geschlossen")]
    [InlineData("New", "Neu")]
    [InlineData("In Progress", "In Bearbeitung")]
    [InlineData("On Hold", "Wartend")]
    [InlineData("Resolved", "Gelöst")]
    public void TranslateStatus_KnownStatuses_ReturnsGerman(string input, string expected)
    {
        TrudeskTranslationHelper.TranslateStatus(input).Should().Be(expected);
    }

    [Fact]
    public void TranslateStatus_UnknownStatus_ReturnsOriginal()
    {
        TrudeskTranslationHelper.TranslateStatus("CustomStatus").Should().Be("CustomStatus");
    }

    [Fact]
    public void TranslateStatus_Null_ReturnsEmptyString()
    {
        TrudeskTranslationHelper.TranslateStatus(null).Should().BeEmpty();
    }

    [Fact]
    public void TranslateStatus_Empty_ReturnsEmpty()
    {
        TrudeskTranslationHelper.TranslateStatus(string.Empty).Should().BeEmpty();
    }

    [Theory]
    [InlineData("Critical", "Kritisch")]
    [InlineData("High", "Hoch")]
    [InlineData("Normal", "Normal")]
    [InlineData("Low", "Niedrig")]
    [InlineData("Urgent", "Dringend")]
    [InlineData("Medium", "Mittel")]
    public void TranslatePriority_KnownPriorities_ReturnsGerman(string input, string expected)
    {
        TrudeskTranslationHelper.TranslatePriority(input).Should().Be(expected);
    }

    [Fact]
    public void TranslatePriority_UnknownPriority_ReturnsOriginal()
    {
        TrudeskTranslationHelper.TranslatePriority("CustomPriority").Should().Be("CustomPriority");
    }

    [Fact]
    public void TranslatePriority_Null_ReturnsEmpty()
    {
        TrudeskTranslationHelper.TranslatePriority(null).Should().BeEmpty();
    }

    [Theory]
    [InlineData("ticket:created", "Ticket erstellt")]
    [InlineData("ticket:updated", "Ticket aktualisiert")]
    [InlineData("ticket:comment:added", "Kommentar hinzugefügt")]
    [InlineData("ticket:deleted", "Ticket gelöscht")]
    [InlineData("ticket:status:updated", "Status geändert")]
    [InlineData("ticket:assignee:set", "Zugewiesen")]
    public void TranslateHistoryAction_KnownActions_ReturnsGerman(string input, string expected)
    {
        TrudeskTranslationHelper.TranslateHistoryAction(input).Should().Be(expected);
    }

    [Fact]
    public void TranslateHistoryAction_UnknownAction_ReturnsOriginal()
    {
        TrudeskTranslationHelper.TranslateHistoryAction("custom:action").Should().Be("custom:action");
    }

    [Fact]
    public void TranslateHistoryAction_Null_ReturnsEmpty()
    {
        TrudeskTranslationHelper.TranslateHistoryAction(null).Should().BeEmpty();
    }

    [Fact]
    public void TranslateTicket_TranslatesStatusAndPriority()
    {
        var ticket = new THWTicketApp.Models.Ticket
        {
            Status = new THWTicketApp.Models.Status { Name = "Open" },
            Priority = new THWTicketApp.Models.Priority { Name = "High" }
        };

        TrudeskTranslationHelper.TranslateTicket(ticket);

        ticket.Status.Name.Should().Be("Offen");
        ticket.Priority.Name.Should().Be("Hoch");
    }

    [Fact]
    public void TranslateTicket_NullStatusAndPriority_DoesNotThrow()
    {
        var ticket = new THWTicketApp.Models.Ticket
        {
            Status = null,
            Priority = null
        };

        var act = () => TrudeskTranslationHelper.TranslateTicket(ticket);
        act.Should().NotThrow();
    }

    [Fact]
    public void TranslateStatus_IsCaseInsensitive()
    {
        TrudeskTranslationHelper.TranslateStatus("open").Should().Be("Offen");
        TrudeskTranslationHelper.TranslateStatus("OPEN").Should().Be("Offen");
        TrudeskTranslationHelper.TranslateStatus("Open").Should().Be("Offen");
    }
}
