using System.Collections.Specialized;
using SocketIOClient;

namespace THWTicketApp.Services;

public class RealtimeService : IDisposable
{
    private readonly AppSettings _appSettings;
    private readonly ITrueDeskApiService _apiService;
    private SocketIOClient.SocketIO? _socket;
    private bool _disposed;
    private CancellationTokenSource? _reconnectCts;
    private int _reconnectAttempt;
    private const int MaxReconnectDelaySeconds = 120;

    public event Action<string>? TicketUpdated;
    public event Action<string>? TicketCreated;
    public event Action<string>? CommentAdded;
    public event Action<bool>? ConnectionStateChanged;

    public bool IsConnected => _socket?.Connected == true;

    public RealtimeService(AppSettings appSettings, ITrueDeskApiService apiService)
    {
        _appSettings = appSettings;
        _apiService = apiService;
    }

    public async Task ConnectAsync()
    {
        if (_socket?.Connected == true) return;
        if (!_apiService.IsAuthenticated) return;

        try
        {
            // Trudesk Socket.IO runs on the same host as the web UI
            var baseUrl = _appSettings.ApiBaseUrl.Replace("/api/v1", "");

            _socket?.Dispose();
            var query = new NameValueCollection
            {
                { "token", await GetTokenAsync() }
            };
            _socket = new SocketIOClient.SocketIO(new Uri(baseUrl), new SocketIOOptions
            {
                Reconnection = true,
                ReconnectionAttempts = 3,
                ConnectionTimeout = TimeSpan.FromSeconds(5),
                Query = query
            });

            // Trudesk emits these events for real-time updates
            _socket.On("updateTickets", ctx =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                    TicketUpdated?.Invoke(string.Empty));
                return Task.CompletedTask;
            });

            _socket.On("updateComments", ctx =>
            {
                var ticketId = TryExtractTicketId(ctx);
                MainThread.BeginInvokeOnMainThread(() =>
                    CommentAdded?.Invoke(ticketId));
                return Task.CompletedTask;
            });

            _socket.On("newTicket", ctx =>
            {
                var ticketId = TryExtractTicketId(ctx);
                MainThread.BeginInvokeOnMainThread(() =>
                    TicketCreated?.Invoke(ticketId));
                return Task.CompletedTask;
            });

            _socket.On("updateAssignee", ctx =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                    TicketUpdated?.Invoke(string.Empty));
                return Task.CompletedTask;
            });

            _socket.On("updateStatus", ctx =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                    TicketUpdated?.Invoke(string.Empty));
                return Task.CompletedTask;
            });

            _socket.OnDisconnected += async (sender, args) =>
            {
                MainThread.BeginInvokeOnMainThread(() => ConnectionStateChanged?.Invoke(false));
                if (!_disposed)
                    await ReconnectWithBackoffAsync();
            };

            _socket.OnConnected += (sender, args) =>
            {
                _reconnectAttempt = 0;
                MainThread.BeginInvokeOnMainThread(() => ConnectionStateChanged?.Invoke(true));
            };

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await _socket.ConnectAsync(cts.Token);
        }
        catch
        {
            // Socket connection is best-effort - app works fine without it
            _ = ReconnectWithBackoffAsync();
        }
    }

    private async Task ReconnectWithBackoffAsync()
    {
        if (_disposed || !_apiService.IsAuthenticated) return;

        _reconnectCts?.Cancel();
        _reconnectCts = new CancellationTokenSource();
        var token = _reconnectCts.Token;

        while (!token.IsCancellationRequested && !_disposed && _apiService.IsAuthenticated)
        {
            _reconnectAttempt++;
            var delaySeconds = Math.Min((int)Math.Pow(2, _reconnectAttempt), MaxReconnectDelaySeconds);

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), token);
                if (token.IsCancellationRequested || _disposed) return;

                if (_socket?.Connected == true) return;

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await _socket!.ConnectAsync(cts.Token);
                return; // Connected successfully
            }
            catch (TaskCanceledException) { return; }
            catch
            {
                // Retry on next iteration
            }
        }
    }

    public async Task DisconnectAsync()
    {
        _reconnectCts?.Cancel();
        _reconnectAttempt = 0;
        if (_socket?.Connected == true)
        {
            await _socket.DisconnectAsync();
        }
    }

    private async Task<string> GetTokenAsync()
    {
        try
        {
            var token = await SecureStorage.GetAsync("auth_token");
            return token ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string TryExtractTicketId(dynamic ctx)
    {
        try
        {
            string raw = ctx.RawText;
            if (string.IsNullOrEmpty(raw)) return string.Empty;

            var doc = System.Text.Json.JsonDocument.Parse(raw);
            var root = doc.RootElement;

            // Try array format [{ ticket: "id" }]
            System.Text.Json.JsonElement data;
            if (root.ValueKind == System.Text.Json.JsonValueKind.Array && root.GetArrayLength() > 0)
                data = root[0];
            else
                data = root;

            if (data.TryGetProperty("ticket", out var ticketEl))
            {
                if (ticketEl.ValueKind == System.Text.Json.JsonValueKind.String)
                    return ticketEl.GetString() ?? string.Empty;
                if (ticketEl.TryGetProperty("_id", out var idEl))
                    return idEl.GetString() ?? string.Empty;
            }
            if (data.TryGetProperty("_id", out var directId))
                return directId.GetString() ?? string.Empty;
        }
        catch { }
        return string.Empty;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _reconnectCts?.Cancel();
        _reconnectCts?.Dispose();
        _socket?.Dispose();
    }
}
