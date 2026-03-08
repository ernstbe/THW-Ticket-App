using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using THWTicketApp.Models;
using THWTicketApp.Models.Responses;

namespace THWTicketApp.Services
{
    public class TrueDeskApiService : ITrueDeskApiService
    {
        private readonly HttpClient _httpClient;
        private readonly AppSettings _settings;
        private string? _authToken;

        public string? CurrentUsername { get; private set; }
        public string? CurrentUserId { get; private set; }
        public string? LastError { get; private set; }

        public TrueDeskApiService(AppSettings settings)
        {
            _settings = settings;
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(_settings.ConnectionTimeoutSeconds)
            };
        }

        public bool IsAuthenticated => !string.IsNullOrEmpty(_authToken);

        public async Task<bool> AuthenticateAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            try
            {
                var payload = new { username, password };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_settings.ApiBaseUrl}/login", content);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var doc = JsonDocument.Parse(json);
                    _authToken = doc.RootElement.GetProperty("accessToken").GetString();
                    if (_httpClient.DefaultRequestHeaders.Contains("accesstoken"))
                    {
                        _httpClient.DefaultRequestHeaders.Remove("accesstoken");
                    }
                    _httpClient.DefaultRequestHeaders.Add("accesstoken", _authToken);
                    // Extract user ID from login response
                    if (doc.RootElement.TryGetProperty("user", out var userEl) &&
                        userEl.TryGetProperty("_id", out var idEl))
                    {
                        CurrentUserId = idEl.GetString();
                    }

                    // Store token and username securely for session persistence
                    CurrentUsername = username;
                    await SecureStorage.SetAsync("auth_token", _authToken ?? string.Empty);
                    await SecureStorage.SetAsync("auth_username", username);
                    await SecureStorage.SetAsync("auth_userid", CurrentUserId ?? string.Empty);
                    return true;
                }
                return false;
            }
            catch (HttpRequestException)
            {
                return false;
            }
            catch (TaskCanceledException)
            {
                return false;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        public async Task<bool> TryRestoreSessionAsync()
        {
            try
            {
                var storedToken = await SecureStorage.GetAsync("auth_token");
                if (!string.IsNullOrEmpty(storedToken))
                {
                    _authToken = storedToken;
                    CurrentUsername = await SecureStorage.GetAsync("auth_username");
                    CurrentUserId = await SecureStorage.GetAsync("auth_userid");
                    if (_httpClient.DefaultRequestHeaders.Contains("accesstoken"))
                    {
                        _httpClient.DefaultRequestHeaders.Remove("accesstoken");
                    }
                    _httpClient.DefaultRequestHeaders.Add("accesstoken", _authToken);

                    // Verify the token is still valid on the server
                    try
                    {
                        var response = await _httpClient.GetAsync($"{_settings.ApiBaseUrl}/login");
                        if (!response.IsSuccessStatusCode)
                        {
                            // Token expired or revoked - clear local state
                            _authToken = null;
                            CurrentUsername = null;
                            CurrentUserId = null;
                            _httpClient.DefaultRequestHeaders.Remove("accesstoken");
                            SecureStorage.Remove("auth_token");
                            SecureStorage.Remove("auth_username");
                            SecureStorage.Remove("auth_userid");
                            return false;
                        }
                    }
                    catch
                    {
                        // Network error - assume token is valid, let it fail on next call
                    }

                    return true;
                }
            }
            catch
            {
                // SecureStorage not available or error reading
            }
            return false;
        }

        public async Task LogoutAsync()
        {
            // Invalidate token on the server
            try
            {
                if (IsAuthenticated)
                {
                    await _httpClient.GetAsync($"{_settings.ApiBaseUrl}/logout");
                }
            }
            catch
            {
                // Best-effort: clear local state even if server call fails
            }

            _authToken = null;
            CurrentUsername = null;
            CurrentUserId = null;
            if (_httpClient.DefaultRequestHeaders.Contains("accesstoken"))
            {
                _httpClient.DefaultRequestHeaders.Remove("accesstoken");
            }
            SecureStorage.Remove("auth_token");
            SecureStorage.Remove("auth_username");
            SecureStorage.Remove("auth_userid");
        }

        // ──────────────────────────────────────────────
        // Tickets
        // ──────────────────────────────────────────────

        public async Task<string> GetTicketsAsync()
        {
            // Trudesk defaults to limit=10; request a large limit to get all tickets
            var response = await _httpClient.GetAsync($"{_settings.ApiBaseUrl}/tickets?limit=1000");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> GetTicketsPagedAsync(int page = 0, int limit = 50)
        {
            var response = await _httpClient.GetAsync($"{_settings.ApiBaseUrl}/tickets?page={page}&limit={limit}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> GetTicketsFilteredAsync(string? status = null, bool? assignedSelf = null, int limit = 1000)
        {
            var queryParts = new List<string> { $"limit={limit}" };
            if (!string.IsNullOrEmpty(status))
                queryParts.Add($"status={Uri.EscapeDataString(status)}");
            if (assignedSelf == true)
                queryParts.Add("assignedself=true");
            var query = string.Join("&", queryParts);
            var response = await _httpClient.GetAsync($"{_settings.ApiBaseUrl}/tickets?{query}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> SearchTicketsAsync(string query)
        {
            var encoded = Uri.EscapeDataString(query);
            var response = await _httpClient.GetAsync($"{_settings.ApiBaseUrl}/tickets/search?search={encoded}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> GetTicketAsync(string ticketUid)
        {
            // Trudesk GET /tickets/:uid expects the numeric uid, not the MongoDB _id
            var response = await _httpClient.GetAsync($"{_settings.ApiBaseUrl}/tickets/{ticketUid}");
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> AddTicketAsync(string title, string description, string? assigneeId)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("Title is required", nameof(title));
            }
            // Trudesk expects: subject, issue, owner; POST /tickets/create
            var payload = new Dictionary<string, object?>
            {
                ["subject"] = title,
                ["issue"] = description,
                ["owner"] = CurrentUserId
            };
            if (!string.IsNullOrEmpty(assigneeId))
                payload["assignee"] = assigneeId;
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_settings.ApiBaseUrl}/tickets/create", content);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
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
                return false;
            }

            var payload = new Dictionary<string, object?>
            {
                ["subject"] = subject,
                ["issue"] = issue ?? string.Empty,
                ["owner"] = CurrentUserId,
            };

            if (!string.IsNullOrEmpty(typeId))
                payload["type"] = typeId;
            if (!string.IsNullOrEmpty(priorityId))
                payload["priority"] = priorityId;
            if (!string.IsNullOrEmpty(groupId))
                payload["group"] = groupId;
            if (!string.IsNullOrEmpty(assigneeId))
                payload["assignee"] = assigneeId;

            var json = JsonSerializer.Serialize(payload);
            System.Diagnostics.Debug.WriteLine($"CreateTicket payload: {json}");
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_settings.ApiBaseUrl}/tickets/create", content);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"CreateTicket failed ({response.StatusCode}): {body}");
                LastError = $"{(int)response.StatusCode}: {body}";
            }
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> EditTicketAsync(Ticket ticket)
        {
            if (ticket == null || string.IsNullOrWhiteSpace(ticket.Id))
            {
                return false;
            }

            var payload = new Dictionary<string, object?>();
            if (ticket.Subject != null)
                payload["subject"] = ticket.Subject;
            if (ticket.Issue != null)
                payload["issue"] = ticket.Issue;
            if (ticket.Priority?.Id != null)
                payload["priority"] = ticket.Priority.Id;
            if (ticket.Status?.Id != null)
                payload["status"] = ticket.Status.Id;
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{_settings.ApiBaseUrl}/tickets/{ticket.Id}", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteTicketAsync(string ticketId)
        {
            if (string.IsNullOrWhiteSpace(ticketId))
                return false;
            var response = await _httpClient.DeleteAsync($"{_settings.ApiBaseUrl}/tickets/{ticketId}");
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateTicketStatusAsync(string ticketId, string statusId)
        {
            if (string.IsNullOrWhiteSpace(ticketId) || string.IsNullOrWhiteSpace(statusId))
                return false;

            var payload = new { status = statusId };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{_settings.ApiBaseUrl}/tickets/{ticketId}", content);
            return response.IsSuccessStatusCode;
        }

        // ──────────────────────────────────────────────
        // Assignment
        // ──────────────────────────────────────────────

        public async Task<bool> AssignTicketAsync(string ticketId, string userId)
        {
            // Trudesk v1 has a permission bug on PUT /tickets/:id/assignee
            // (route uses 'ticket:setAssignee' which doesn't match 'tickets:*' grant).
            // Use the general update endpoint instead which supports the assignee field.
            var payload = new { assignee = userId };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{_settings.ApiBaseUrl}/tickets/{ticketId}", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ClearTicketAssigneeAsync(string ticketId)
        {
            // Clear assignee by setting it to empty via the general update endpoint
            var payload = new { assignee = (string?)null };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{_settings.ApiBaseUrl}/tickets/{ticketId}", content);
            return response.IsSuccessStatusCode;
        }

        // ──────────────────────────────────────────────
        // Comments & Notes
        // ──────────────────────────────────────────────

        public async Task<bool> AddCommentAsync(string id, string ownerId, string newComment)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(newComment))
            {
                return false;
            }

            var payload = new { _id = id, ownerId, comment = newComment };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_settings.ApiBaseUrl}/tickets/addcomment", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> AddNoteAsync(string ticketId, string ownerId, string note)
        {
            if (string.IsNullOrWhiteSpace(ticketId) || string.IsNullOrWhiteSpace(note))
                return false;

            var payload = new { ticketid = ticketId, owner = ownerId, note };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_settings.ApiBaseUrl}/tickets/addnote", content);
            return response.IsSuccessStatusCode;
        }

        // ──────────────────────────────────────────────
        // Attachments
        // ──────────────────────────────────────────────

        public async Task<bool> UploadAttachmentAsync(string ticketId, Stream fileStream, string fileName)
        {
            using var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                GetMimeType(fileName));
            content.Add(streamContent, "file", fileName);
            content.Add(new StringContent(ticketId), "ticketId");

            // Upload is a web route (not under /api/v1), requires session auth
            var baseUrl = _settings.ApiBaseUrl.Replace("/api/v1", "");
            var response = await _httpClient.PostAsync($"{baseUrl}/tickets/uploadattachment", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<Stream?> DownloadAttachmentAsync(string attachmentPath)
        {
            var baseUrl = _settings.ApiBaseUrl.Replace("/api/v1", "");
            var response = await _httpClient.GetAsync($"{baseUrl}{attachmentPath}");
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadAsStreamAsync();
            return null;
        }

        public string GetAttachmentUrl(string attachmentPath)
        {
            var baseUrl = _settings.ApiBaseUrl.Replace("/api/v1", "");
            return $"{baseUrl}{attachmentPath}";
        }

        public async Task<bool> DeleteAttachmentAsync(string ticketId, string attachmentId)
        {
            // Trudesk route: DELETE /tickets/:tid/attachments/remove/:aid
            var response = await _httpClient.DeleteAsync(
                $"{_settings.ApiBaseUrl}/tickets/{ticketId}/attachments/remove/{attachmentId}");
            return response.IsSuccessStatusCode;
        }

        // ──────────────────────────────────────────────
        // Reference Data
        // ──────────────────────────────────────────────

        public async Task<string> GetStatusesAsync()
        {
            var response = await _httpClient.GetAsync($"{_settings.ApiBaseUrl}/tickets/status");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> GetUsersAsync()
        {
            var response = await _httpClient.GetAsync($"{_settings.ApiBaseUrl}/users");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> GetAssigneesAsync()
        {
            // Dedicated endpoint returns only users with agent/admin roles
            var response = await _httpClient.GetAsync($"{_settings.ApiBaseUrl}/users/getassignees");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> GetTicketTypesAsync()
        {
            var response = await _httpClient.GetAsync($"{_settings.ApiBaseUrl}/tickets/types");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> GetPrioritiesAsync()
        {
            var response = await _httpClient.GetAsync($"{_settings.ApiBaseUrl}/tickets/priorities");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> GetTagsAsync()
        {
            var response = await _httpClient.GetAsync($"{_settings.ApiBaseUrl}/tags/limit");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> GetGroupsAsync()
        {
            var response = await _httpClient.GetAsync($"{_settings.ApiBaseUrl}/groups");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        // ──────────────────────────────────────────────
        // Tickets by Group
        // ──────────────────────────────────────────────

        public async Task<string> GetTicketsByGroupAsync(string groupId, int page = 0, int limit = 50)
        {
            var response = await _httpClient.GetAsync(
                $"{_settings.ApiBaseUrl}/tickets/group/{groupId}?page={page}&limit={limit}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        // ──────────────────────────────────────────────
        // Overdue Tickets
        // ──────────────────────────────────────────────

        public async Task<string> GetOverdueTicketsAsync()
        {
            var response = await _httpClient.GetAsync($"{_settings.ApiBaseUrl}/tickets/overdue");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        // ──────────────────────────────────────────────
        // Subscriptions
        // ──────────────────────────────────────────────

        public async Task<bool> SubscribeToTicketAsync(string ticketId, bool subscribe)
        {
            if (string.IsNullOrWhiteSpace(ticketId) || string.IsNullOrEmpty(CurrentUserId))
                return false;

            var payload = new { user = CurrentUserId, subscribe };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{_settings.ApiBaseUrl}/tickets/{ticketId}/subscribe", content);
            return response.IsSuccessStatusCode;
        }

        // ──────────────────────────────────────────────
        // Notifications
        // ──────────────────────────────────────────────

        public async Task<string> GetNotificationsAsync()
        {
            var response = await _httpClient.GetAsync($"{_settings.ApiBaseUrl}/users/notifications");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<int> GetNotificationCountAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_settings.ApiBaseUrl}/users/notificationCount");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("count", out var countEl))
                {
                    // Trudesk returns count as string, e.g. "70"
                    if (countEl.ValueKind == JsonValueKind.String)
                        return int.TryParse(countEl.GetString(), out var c) ? c : 0;
                    return countEl.GetInt32();
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        // ──────────────────────────────────────────────
        // Statistics
        // ──────────────────────────────────────────────

        public async Task<string> GetTicketStatsAsync(int timespan = 30)
        {
            var response = await _httpClient.GetAsync($"{_settings.ApiBaseUrl}/tickets/stats/{timespan}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> GetTicketStatsForGroupAsync(string groupId)
        {
            var response = await _httpClient.GetAsync($"{_settings.ApiBaseUrl}/tickets/stats/group/{groupId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> GetTicketStatsForUserAsync(string userId)
        {
            var response = await _httpClient.GetAsync($"{_settings.ApiBaseUrl}/tickets/stats/user/{userId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        // ──────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────

        private static string GetMimeType(string fileName)
        {
            var ext = System.IO.Path.GetExtension(fileName)?.ToLowerInvariant();
            return ext switch
            {
                ".pdf" => "application/pdf",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".txt" => "text/plain",
                ".zip" => "application/zip",
                _ => "application/octet-stream"
            };
        }
    }
}
