using FluentAssertions;
using THWTicketApp.Helpers;
using Xunit;

namespace THWTicketApp.Tests.Helpers;

public class TranslatorTests
{
    // --- Ticket Types ---

    [Theory]
    [InlineData("Task", "Aufgabe")]
    [InlineData("Issue", "Problem")]
    [InlineData("Bug", "Fehler")]
    [InlineData("Request", "Anfrage")]
    [InlineData("Feature", "Funktion")]
    [InlineData("Feature Request", "Funktionsanfrage")]
    [InlineData("Incident", "Vorfall")]
    [InlineData("Service Request", "Serviceanfrage")]
    [InlineData("Question", "Frage")]
    [InlineData("Support", "Unterstützung")]
    [InlineData("Maintenance", "Wartung")]
    [InlineData("Change Request", "Änderungsanfrage")]
    [InlineData("Problem", "Problem")]
    public void Translate_TicketTypes_ReturnsGerman(string input, string expected)
    {
        Translator.Translate(input).Should().Be(expected);
    }

    // --- Priorities ---

    [Theory]
    [InlineData("Low", "Niedrig")]
    [InlineData("Normal", "Normal")]
    [InlineData("Medium", "Mittel")]
    [InlineData("High", "Hoch")]
    [InlineData("Critical", "Kritisch")]
    [InlineData("Urgent", "Dringend")]
    [InlineData("Very High", "Sehr Hoch")]
    [InlineData("Very Low", "Sehr Niedrig")]
    [InlineData("Emergency", "Notfall")]
    [InlineData("Blocker", "Blockierend")]
    [InlineData("Minor", "Gering")]
    [InlineData("Major", "Wichtig")]
    [InlineData("Trivial", "Trivial")]
    public void Translate_Priorities_ReturnsGerman(string input, string expected)
    {
        Translator.Translate(input).Should().Be(expected);
    }

    // --- Statuses ---

    [Theory]
    [InlineData("New", "Neu")]
    [InlineData("Open", "Offen")]
    [InlineData("Pending", "In Bearbeitung")]
    [InlineData("In Progress", "In Bearbeitung")]
    [InlineData("Waiting", "Wartend")]
    [InlineData("On Hold", "Zurückgestellt")]
    [InlineData("Resolved", "Gelöst")]
    [InlineData("Closed", "Geschlossen")]
    [InlineData("Cancelled", "Abgebrochen")]
    [InlineData("Rejected", "Abgelehnt")]
    [InlineData("Reopened", "Wiedereröffnet")]
    [InlineData("Assigned", "Zugewiesen")]
    [InlineData("Unassigned", "Nicht zugewiesen")]
    [InlineData("Completed", "Abgeschlossen")]
    [InlineData("Done", "Erledigt")]
    [InlineData("Approved", "Genehmigt")]
    [InlineData("Denied", "Abgelehnt")]
    [InlineData("Review", "In Prüfung")]
    [InlineData("In Review", "In Prüfung")]
    [InlineData("Testing", "Im Test")]
    [InlineData("Verified", "Verifiziert")]
    public void Translate_Statuses_ReturnsGerman(string input, string expected)
    {
        Translator.Translate(input).Should().Be(expected);
    }

    // --- Case insensitivity ---

    [Theory]
    [InlineData("low", "Niedrig")]
    [InlineData("LOW", "Niedrig")]
    [InlineData("Low", "Niedrig")]
    [InlineData("high", "Hoch")]
    [InlineData("HIGH", "Hoch")]
    [InlineData("new", "Neu")]
    [InlineData("NEW", "Neu")]
    [InlineData("task", "Aufgabe")]
    [InlineData("TASK", "Aufgabe")]
    [InlineData("critical", "Kritisch")]
    public void Translate_IsCaseInsensitive(string input, string expected)
    {
        Translator.Translate(input).Should().Be(expected);
    }

    // --- Edge cases ---

    [Fact]
    public void Translate_Null_ReturnsEmptyString()
    {
        Translator.Translate(null).Should().BeEmpty();
    }

    [Fact]
    public void Translate_EmptyString_ReturnsEmptyString()
    {
        Translator.Translate(string.Empty).Should().BeEmpty();
    }

    [Theory]
    [InlineData("UnknownValue")]
    [InlineData("Something Else")]
    [InlineData("Random Text 123")]
    public void Translate_UnknownText_ReturnsOriginal(string input)
    {
        Translator.Translate(input).Should().Be(input);
    }
}
