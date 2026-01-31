namespace THWTicketApp.Helpers;

/// <summary>
/// Statische Klasse zum Übersetzen von englischen Begriffen ins Deutsche.
/// </summary>
public static class Translator
{
    private static readonly Dictionary<string, string> Translations = new(StringComparer.OrdinalIgnoreCase)
    {
        // Ticket-Typen
        { "Task", "Aufgabe" },
        { "Issue", "Problem" },
        { "Bug", "Fehler" },
        { "Request", "Anfrage" },
        { "Feature", "Funktion" },
        { "Feature Request", "Funktionsanfrage" },
        { "Incident", "Vorfall" },
        { "Service Request", "Serviceanfrage" },
        { "Question", "Frage" },
        { "Support", "Unterstützung" },
        { "Maintenance", "Wartung" },
        { "Change Request", "Änderungsanfrage" },
        { "Problem", "Problem" },

        // Prioritäten
        { "Low", "Niedrig" },
        { "Normal", "Normal" },
        { "Medium", "Mittel" },
        { "High", "Hoch" },
        { "Critical", "Kritisch" },
        { "Urgent", "Dringend" },
        { "Very High", "Sehr Hoch" },
        { "Very Low", "Sehr Niedrig" },
        { "Emergency", "Notfall" },
        { "Blocker", "Blockierend" },
        { "Minor", "Gering" },
        { "Major", "Wichtig" },
        { "Trivial", "Trivial" },

        // Status
        { "New", "Neu" },
        { "Open", "Offen" },
        { "Pending", "In Bearbeitung" },
        { "In Progress", "In Bearbeitung" },
        { "Waiting", "Wartend" },
        { "On Hold", "Zurückgestellt" },
        { "Resolved", "Gelöst" },
        { "Closed", "Geschlossen" },
        { "Cancelled", "Abgebrochen" },
        { "Rejected", "Abgelehnt" },
        { "Reopened", "Wiedereröffnet" },
        { "Assigned", "Zugewiesen" },
        { "Unassigned", "Nicht zugewiesen" },
        { "Completed", "Abgeschlossen" },
        { "Done", "Erledigt" },
        { "Approved", "Genehmigt" },
        { "Denied", "Abgelehnt" },
        { "Review", "In Prüfung" },
        { "In Review", "In Prüfung" },
        { "Testing", "Im Test" },
        { "Verified", "Verifiziert" }
    };

    /// <summary>
    /// Übersetzt einen englischen Begriff ins Deutsche.
    /// Falls keine Übersetzung gefunden wird, wird der Originaltext zurückgegeben.
    /// </summary>
    public static string Translate(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return text ?? string.Empty;

        return Translations.TryGetValue(text, out var translation) ? translation : text;
    }
}
