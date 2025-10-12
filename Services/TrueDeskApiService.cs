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
        private string _authToken;
        private const string BaseUrl = "http://192.168.178.20:8118/api/v1"; // Replace with actual URL

        public TrueDeskApiService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<bool> AuthenticateAsync(string username, string password)
        {
            try
            {


                var payload = new { username, password };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{BaseUrl}/login", content);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var doc = JsonDocument.Parse(json);
                    _authToken = doc.RootElement.GetProperty("accessToken").GetString();
                    _httpClient.DefaultRequestHeaders.Add("accesstoken", _authToken);
                    return true;
                }
                return false;
            }
            catch (System.Exception)
            {

                throw;
            }
        }

        public async Task<string> GetTicketsAsync()
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/tickets");
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> AddTicketAsync(string title, string description, int assignedUserId)
        {
            var payload = new { title, description, assignedUserId };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{BaseUrl}/tickets", content);
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<bool> AssignTicketAsync(string ticketId, string userId)
        {
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

        public async Task<string> GetTicketAsync(string ticketId)
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/tickets/{ticketId}");
            return await response.Content.ReadAsStringAsync();
        }

        internal async Task<bool> EditTicketAsync(Ticket ticket)
        {
            await Task.CompletedTask;
            return true;
        }

        internal async Task<string> GetUsers()
        {
            var response = _httpClient.GetAsync($"{BaseUrl}/users").Result;
            return await response.Content.ReadAsStringAsync();
        }
    }
}
