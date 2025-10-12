using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using THWTicketApp.Models;
using THWTicketApp.Models.Responses;

namespace THWTicketApp.Services
{
    public class TrueDeskApiService
    {
        private readonly HttpClient _httpClient;
        private readonly AppSettings _settings;
        private string? _authToken;

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
                    // Store token securely for session persistence
                    await SecureStorage.SetAsync("auth_token", _authToken ?? string.Empty);
                    return true;
                }
                return false;
                    }
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
            if (_httpClient.DefaultRequestHeaders.Contains("accesstoken"))
            {
                _httpClient.DefaultRequestHeaders.Remove("accesstoken");
            }
            SecureStorage.Remove("auth_token");
>>>>>>> 5748322 (Initial commit: THW Ticket App - .NET MAUI cross-platform application)
        }

        public async Task<string> GetTicketsAsync()
        {
            var response = await _httpClient.GetAsync($"{_settings.ApiBaseUrl}/tickets");
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
<<<<<<< HEAD
<<<<<<< HEAD
            // Use the dedicated assignee endpoint: PUT /tickets/{id}/assignee
            var payload = new { assignee = userId };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{BaseUrl}/tickets/{ticketId}/assignee", content);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Clear the assignee for a ticket using DELETE /tickets/{id}/assignee
        /// </summary>
        public async Task<bool> ClearTicketAssigneeAsync(string ticketId)
        {
            var response = await _httpClient.DeleteAsync($"{BaseUrl}/tickets/{ticketId}/assignee");
            return response.IsSuccessStatusCode;
        }

        internal async Task<bool> AddCommentAsync(string id, string ownerId, string newComment)
        {
            var payload = new { ticketId = id, owner = ownerId, comment = newComment };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{BaseUrl}/tickets/addcomment", content);
            return response.IsSuccessStatusCode;
        }

                    // Use the dedicated assignee endpoint: PUT /tickets/{id}/assignee
                    var payload = new { assignee = userId };
                    var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                    var response = await _httpClient.PutAsync($"{_settings.ApiBaseUrl}/tickets/{ticketId}/assignee", content);
                    return response.IsSuccessStatusCode;
                }

                /// <summary>
                /// Clear the assignee for a ticket using DELETE /tickets/{id}/assignee
                /// </summary>
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

                public async Task<string> GetGroupsAsync()
                {
                    var response = await _httpClient.GetAsync($"{_settings.ApiBaseUrl}/groups");
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
        {
