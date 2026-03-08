// Stubs for MAUI Essentials types so that SyncService.cs can compile
// in the net10.0 test project without referencing the MAUI SDK.

// ReSharper disable CheckNamespace

namespace Microsoft.Maui.Networking
{
    public enum NetworkAccess
    {
        Unknown,
        None,
        Local,
        ConstrainedInternet,
        Internet
    }

    public class ConnectivityChangedEventArgs : EventArgs
    {
        public NetworkAccess NetworkAccess { get; set; }
    }

    public interface IConnectivity
    {
        NetworkAccess NetworkAccess { get; }
        event EventHandler<ConnectivityChangedEventArgs> ConnectivityChanged;
    }

    /// <summary>
    /// Stub for Microsoft.Maui.Networking.Connectivity.
    /// Tests can set <see cref="SetCurrent"/> to control behaviour.
    /// </summary>
    public static class Connectivity
    {
        private static IConnectivity? _current;

        public static IConnectivity Current
        {
            get => _current ?? throw new InvalidOperationException(
                "Connectivity.Current has not been set. Call Connectivity.SetCurrent() first.");
        }

        /// <summary>Allow tests to inject a fake IConnectivity.</summary>
        public static void SetCurrent(IConnectivity connectivity) => _current = connectivity;

        // SyncService subscribes to the static event Connectivity.ConnectivityChanged
        public static event EventHandler<ConnectivityChangedEventArgs>? ConnectivityChanged;

        public static void RaiseConnectivityChanged(ConnectivityChangedEventArgs args)
        {
            ConnectivityChanged?.Invoke(null, args);
        }
    }
}

namespace Microsoft.Maui.ApplicationModel
{
    /// <summary>
    /// Stub for Microsoft.Maui.ApplicationModel.MainThread.
    /// In tests, <see cref="BeginInvokeOnMainThread"/> simply invokes the action directly.
    /// </summary>
    public static class MainThread
    {
        public static void BeginInvokeOnMainThread(Action action) => action();
    }
}
