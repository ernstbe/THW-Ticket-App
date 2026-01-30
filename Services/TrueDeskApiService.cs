using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using THWTicketApp.Models;
using THWTicketApp.Models.Responses;

namespace THWTicketApp.Services
{
    public class TrueDeskApiService
    {
        private readonly HttpClient _httpClient;
        private readonly AppSettings _settings;
        private readonly ILogger<TrueDeskApiService> _logger;
        private string? _authToken;

        public TrueDeskApiService(AppSettings settings, ILogger<TrueDeskApiService> logger)
        {
            _settings = settings;
            _logger = logger;
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(_settings.ConnectionTimeoutSeconds)
            };
            _logger.LogInformation("TrueDeskApiService initialized with base URL: {BaseUrl}", _settings.ApiBaseUrl);
        }

        public bool IsAuthenticated => !string.IsNullOrEmpty(_authToken);

        public async Task<bool> AuthenticateAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                _logger.LogWarning("Authentication failed: username or password is empty");
                return false;
            }

            _logger.LogInformation("Attempting authentication for user: {Username}", username);

            try
            {
                var payload = new { username, password };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_settings.ApiBaseUrl}/login", content);

                _logger.LogDebug("Login response status: {StatusCode}", response.StatusCode);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var doc = JsonDocument.Parse(json);

                    if (doc.RootElement.TryGetProperty("accessToken", out var tokenElement))
                    {
                        _authToken = tokenElement.GetString();

                        // Remove existing header if present before adding
                        if (_httpClient.DefaultRequestHeaders.Contains("accesstoken"))
                        {
                            _httpClient.DefaultRequestHeaders.Remove("accesstoken");
                        }
                        _httpClient.DefaultRequestHeaders.Add("accesstoken", _authToken);

                        // Store token securely for session persistence
                        await SecureStorage.SetAsync("auth_token", _authToken ?? string.Empty);

                        _logger.LogInformation("Authentication successful for user: {Username}", username);
                        return true;
                    }
                }
                _logger.LogWarning("Authentication failed for user: {Username} - Invalid response", username);
                return false;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Authentication failed: Network error");
                return false;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Authentication failed: Request timeout");
                return false;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Authentication failed: Invalid JSON response");
                return false;
            }
        }

        public async Task<bool> TryRestoreSessionAsync()
        {
            _logger.LogDebug("Attempting to restore session from secure storage");
            try
            {
                var storedToken = await SecureStorage.GetAsync("auth_token");
                if (!string.IsNullOrEmpty(storedToken))
                {
                    _authToken = storedToken;
                    if (_httpClient.DefaultRequestHeaders.Contains("accesstoken"))
                    {
                        _httpClient.DefaultRequestHeaders.Remove("accesstoken");
                    }
                    _httpClient.DefaultRequestHeaders.Add("accesstoken", _authToken);
                    _logger.LogInformation("Session restored successfully");
                    return true;
                }
                _logger.LogDebug("No stored session found");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to restore session from secure storage");
            }
            return false;
        }

        public void Logout()
        {
            _logger.LogInformation("User logged out");
            _authToken = null;
            if (_httpClient.DefaultRequestHeaders.Contains("accesstoken"))
            {
                _httpClient.DefaultRequestHeaders.Remove("accesstoken");
            }
            SecureStorage.Remove("auth_token");
        }

        public async Task<string> GetTicketsAsync()
        {
            _logger.LogDebug("Fetching tickets from API");
            var response = await _httpClient.GetAsync($"{_settings.ApiBaseUrl}/tickets");
            _logger.LogDebug("GetTickets response: {StatusCode}", response.StatusCode);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> AddTicketAsync(string title, string description, int assignedUserId)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("Title is required", nameof(title));
            }

            var payload = new { title, description, assignedUserId };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_settings.ApiBaseUrl}/tickets", content);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<bool> AssignTicketAsync(string ticketId, string userId)
        {
            if (string.IsNullOrWhiteSpace(ticketId) || string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("AssignTicket failed: ticketId or userId is empty");
                return false;
            }

            _logger.LogInformation("Assigning ticket {TicketId} to user {UserId}", ticketId, userId);
            var payload = new { assignee = userId };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{_settings.ApiBaseUrl}/tickets/{ticketId}", content);
            _logger.LogDebug("AssignTicket response: {StatusCode}", response.StatusCode);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> AddCommentAsync(string ticketId, string newComment)
        {
            if (string.IsNullOrWhiteSpace(ticketId) || string.IsNullOrWhiteSpace(newComment))
            {
                _logger.LogWarning("AddComment failed: ticketId or comment is empty");
                return false;
            }

            _logger.LogInformation("Adding comment to ticket {TicketId}", ticketId);
            var payload = new { _id = ticketId, comment = newComment };
            _logger.LogDebug("AddComment payload: {Payload}", JsonSerializer.Serialize(payload));
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_settings.ApiBaseUrl}/tickets/addcomment", content);
            _logger.LogDebug("AddComment response: {StatusCode}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("AddComment failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
            }

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> EditTicketAsync(Ticket ticket)
        {
            if (ticket == null || string.IsNullOrWhiteSpace(ticket.Id))
            {
                return false;
            }

            var payload = new
            {
                subject = ticket.Subject,
                issue = ticket.Issue,
                priority = ticket.Priority?.Id,
                status = ticket.Status?.Id
            };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{_settings.ApiBaseUrl}/tickets/{ticket.Id}", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<string> GetUsersAsync()
        {
            _logger.LogDebug("Fetching users from API");
            var response = await _httpClient.GetAsync($"{_settings.ApiBaseUrl}/users");
            _logger.LogDebug("GetUsers response: {StatusCode}", response.StatusCode);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> GetTicketTypesAsync()
        {
            _logger.LogDebug("Fetching ticket types from API");
            var response = await _httpClient.GetAsync($"{_settings.ApiBaseUrl}/tickets/types");
            _logger.LogDebug("GetTicketTypes response: {StatusCode}", response.StatusCode);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> GetGroupsAsync()
        {
            _logger.LogDebug("Fetching groups from API");
            var response = await _httpClient.GetAsync($"{_settings.ApiBaseUrl}/groups");
            _logger.LogDebug("GetGroups response: {StatusCode}", response.StatusCode);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> GetStatusesAsync()
        {
            _logger.LogDebug("Fetching statuses from API");
            var response = await _httpClient.GetAsync($"{_settings.ApiBaseUrl}/tickets/status");
            _logger.LogDebug("GetStatuses response: {StatusCode}", response.StatusCode);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<bool> UpdateTicketStatusAsync(string ticketId, string statusId)
        {
            if (string.IsNullOrWhiteSpace(ticketId) || string.IsNullOrWhiteSpace(statusId))
            {
                _logger.LogWarning("UpdateTicketStatus failed: ticketId or statusId is empty");
                return false;
            }

            _logger.LogInformation("Updating ticket {TicketId} status to {StatusId}", ticketId, statusId);
            var payload = new { status = statusId };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{_settings.ApiBaseUrl}/tickets/{ticketId}", content);
            _logger.LogDebug("UpdateTicketStatus response: {StatusCode}", response.StatusCode);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> CreateTicketAsync(
            string subject,
            string? issue,
            string? typeId,
            string? priorityId,
            string? groupId,
            string? assigneeId)
        {
            if (string.IsNullOrWhiteSpace(subject))
            {
                _logger.LogWarning("CreateTicket failed: subject is empty");
                return false;
            }

            _logger.LogInformation("Creating ticket with subject: {Subject}", subject);

            var payload = new Dictionary<string, object?>
            {
                ["subject"] = subject,
                ["issue"] = string.IsNullOrWhiteSpace(issue) ? subject : issue,
            };

            if (!string.IsNullOrEmpty(typeId))
                payload["type"] = typeId;
            if (!string.IsNullOrEmpty(priorityId))
                payload["priority"] = priorityId;
            if (!string.IsNullOrEmpty(groupId))
                payload["group"] = groupId;
            if (!string.IsNullOrEmpty(assigneeId))
                payload["assignee"] = assigneeId;

            _logger.LogDebug("CreateTicket payload: {Payload}", JsonSerializer.Serialize(payload));
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_settings.ApiBaseUrl}/tickets/create", content);
            _logger.LogDebug("CreateTicket response: {StatusCode}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("CreateTicket failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
            }

            return response.IsSuccessStatusCode;
        }
    }
}
