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
                // Network or connection error
                return false;
            }
            catch (TaskCanceledException)
            {
                // Timeout
                return false;
            }
            catch (JsonException)
            {
                // Invalid response format
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
                    return true;
                }
            }
            catch
            {
                // SecureStorage not available or error reading
            }
            return false;
        }

        public void Logout()
        {
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

        public async Task<string> GetTicketsAsync()
        {
            var response = await _httpClient.GetAsync($"{_settings.ApiBaseUrl}/tickets");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> GetTicketsPagedAsync(int page = 0, int limit = 50)
        {
            var response = await _httpClient.GetAsync($"{_settings.ApiBaseUrl}/tickets?page={page}&limit={limit}");
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
            // Use the dedicated assignee endpoint: PUT /tickets/{id}/assignee
            var payload = new { assignee = userId };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{_settings.ApiBaseUrl}/tickets/{ticketId}/assignee", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ClearTicketAssigneeAsync(string ticketId)
        {
            var response = await _httpClient.DeleteAsync($"{_settings.ApiBaseUrl}/tickets/{ticketId}/assignee");
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> AddCommentAsync(string id, string ownerId, string newComment)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(newComment))
            {
                return false;
            }

            var payload = new { ticketId = id, owner = ownerId, comment = newComment };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_settings.ApiBaseUrl}/tickets/addcomment", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<string> GetTicketAsync(string ticketId)
        {
            var response = await _httpClient.GetAsync($"{_settings.ApiBaseUrl}/tickets/{ticketId}");
            return await response.Content.ReadAsStringAsync();
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

        public async Task<bool> AddNoteAsync(string ticketId, string ownerId, string note)
        {
            if (string.IsNullOrWhiteSpace(ticketId) || string.IsNullOrWhiteSpace(note))
                return false;

            var payload = new { ticketid = ticketId, owner = ownerId, note };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_settings.ApiBaseUrl}/tickets/addnote", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<string> GetStatusesAsync()
        {
            var response = await _httpClient.GetAsync($"{_settings.ApiBaseUrl}/tickets/statuses");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> GetUsersAsync()
        {
            var response = await _httpClient.GetAsync($"{_settings.ApiBaseUrl}/users");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> GetTicketTypesAsync()
        {
            var response = await _httpClient.GetAsync($"{_settings.ApiBaseUrl}/tickets/types");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> GetTagsAsync()
        {
            var response = await _httpClient.GetAsync($"{_settings.ApiBaseUrl}/tags");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> GetGroupsAsync()
        {
            var response = await _httpClient.GetAsync($"{_settings.ApiBaseUrl}/groups");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<bool> UploadAttachmentAsync(string ticketId, Stream fileStream, string fileName)
        {
            using var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                GetMimeType(fileName));
            content.Add(streamContent, "file", fileName);
            content.Add(new StringContent(ticketId), "ticketId");

            var response = await _httpClient.PostAsync($"{_settings.ApiBaseUrl}/tickets/uploadattachment", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<Stream?> DownloadAttachmentAsync(string attachmentPath)
        {
            // Trudesk serves attachments relative to the API base
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
            var response = await _httpClient.DeleteAsync(
                $"{_settings.ApiBaseUrl}/tickets/{ticketId}/attachments/{attachmentId}");
            return response.IsSuccessStatusCode;
        }

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

        public async Task<bool> UpdateTicketStatusAsync(string ticketId, string statusId)
        {
            if (string.IsNullOrWhiteSpace(ticketId) || string.IsNullOrWhiteSpace(statusId))
                return false;

            var payload = new { status = statusId };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{_settings.ApiBaseUrl}/tickets/{ticketId}", content);
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
                return false;
            }

            var payload = new Dictionary<string, object?>
            {
                ["subject"] = subject,
                ["issue"] = issue ?? string.Empty,
            };

            if (!string.IsNullOrEmpty(typeId))
                payload["type"] = typeId;
            if (!string.IsNullOrEmpty(priorityId))
                payload["priority"] = priorityId;
            if (!string.IsNullOrEmpty(groupId))
                payload["group"] = groupId;
            if (!string.IsNullOrEmpty(assigneeId))
                payload["assignee"] = assigneeId;

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_settings.ApiBaseUrl}/tickets/create", content);
            return response.IsSuccessStatusCode;
        }
    }
}
