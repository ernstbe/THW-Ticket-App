// Stub for Microsoft.Maui.Storage.SecureStorage so that TrueDeskApiService.cs
// can compile in the net10.0 test project without referencing the MAUI SDK.

// ReSharper disable once CheckNamespace
namespace Microsoft.Maui.Storage;

/// <summary>
/// In-memory stub for SecureStorage used in unit tests.
/// Call <see cref="Clear"/> between tests to reset state.
/// </summary>
public static class SecureStorage
{
    private static readonly Dictionary<string, string> Store = new();

    public static Task SetAsync(string key, string value)
    {
        Store[key] = value;
        return Task.CompletedTask;
    }

    public static Task<string?> GetAsync(string key)
    {
        Store.TryGetValue(key, out var value);
        return Task.FromResult(value);
    }

    public static bool Remove(string key) => Store.Remove(key);

    public static void RemoveAll() => Store.Clear();

    public static void Clear() => Store.Clear();
}
