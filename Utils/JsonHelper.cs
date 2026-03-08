using System.Text.Json;

namespace THWTicketApp.Utils;

/// <summary>
/// Helpers for parsing Trudesk API responses that may be wrapped in
/// <c>{"success":true,"propertyName":[...]}</c> or returned as raw arrays.
/// </summary>
public static class JsonHelper
{
    private static readonly JsonSerializerOptions DefaultOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Deserializes an array that may be wrapped in an object under the given property name.
    /// For example, <c>{"success":true,"status":[...]}</c> with propertyName "status".
    /// </summary>
    public static T[] DeserializeWrappedArray<T>(string json, string propertyName, JsonSerializerOptions? options = null)
    {
        options ??= DefaultOptions;
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty(propertyName, out var el) && el.ValueKind == JsonValueKind.Array)
            return JsonSerializer.Deserialize<T[]>(el.GetRawText(), options) ?? [];
        // Fallback: try parsing the whole thing as array
        return JsonSerializer.Deserialize<T[]>(json, options) ?? [];
    }
}
