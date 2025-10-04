using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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

        public async Task<string> AssignTicketAsync(int ticketId, int userId)
        {
            var payload = new { userId };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{BaseUrl}/tickets/{ticketId}/assign", content);
            return await response.Content.ReadAsStringAsync();
        }
    }
}
