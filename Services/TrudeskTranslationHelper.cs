namespace THWTicketApp.Services;

/// <summary>
/// Maps English Trudesk API values to German translations (from Trudesk i18n de/translation.json).
/// </summary>
public static class TrudeskTranslationHelper
{
    private static readonly Dictionary<string, string> PriorityTranslations = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Normal"] = "Normal",
        ["Low"] = "Niedrig",
        ["Medium"] = "Mittel",
        ["High"] = "Hoch",
        ["Urgent"] = "Dringend",
        ["Critical"] = "Kritisch"
    };

    private static readonly Dictionary<string, string> StatusTranslations = new(StringComparer.OrdinalIgnoreCase)
    {
        ["New"] = "Neu",
        ["Open"] = "Offen",
        ["Pending"] = "Ausstehend",
        ["Closed"] = "Geschlossen",
        ["In Progress"] = "In Bearbeitung",
        ["On Hold"] = "Wartend",
        ["Resolved"] = "Gelöst"
    };

    public static string TranslatePriority(string? name)
    {
        if (string.IsNullOrEmpty(name)) return name ?? string.Empty;
        return PriorityTranslations.TryGetValue(name, out var translated) ? translated : name;
    }

    public static string TranslateStatus(string? name)
    {
        if (string.IsNullOrEmpty(name)) return name ?? string.Empty;
        return StatusTranslations.TryGetValue(name, out var translated) ? translated : name;
    }

    /// <summary>
    /// Translates Status.Name and Priority.Name on a ticket in-place.
    /// </summary>
    public static void TranslateTicket(Models.Ticket ticket)
    {
        if (ticket.Status != null && !string.IsNullOrEmpty(ticket.Status.Name))
        {
            ticket.Status.Name = TranslateStatus(ticket.Status.Name);
        }

        if (ticket.Priority != null && !string.IsNullOrEmpty(ticket.Priority.Name))
        {
            ticket.Priority.Name = TranslatePriority(ticket.Priority.Name);
        }
    }
}
