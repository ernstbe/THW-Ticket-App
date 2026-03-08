using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using THWTicketApp.Models;
using THWTicketApp.Services;
using Xunit;

namespace THWTicketApp.Tests.Services;

/// <summary>
/// A mock HTTP message handler that queues responses and captures sent requests.
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _responses = new();
    public List<HttpRequestMessage> SentRequests { get; } = new();

    /// <summary>
    /// Captured content headers per request. Index matches SentRequests.
    /// Each entry is a list of (name, contentType, fileName) tuples for multipart content,
    /// or a single entry with the body string for non-multipart content.
    /// </summary>
    public List<List<(string? Name, string? ContentType, string? FileName, string Body)>> CapturedContentParts { get; } = new();

    public void EnqueueResponse(HttpStatusCode status, string content = "")
    {
        _responses.Enqueue(new HttpResponseMessage(status)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        });
    }

    public void EnqueueResponse(HttpResponseMessage response)
    {
        _responses.Enqueue(response);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        SentRequests.Add(request);

        // Capture content before it might be disposed
        var parts = new List<(string? Name, string? ContentType, string? FileName, string Body)>();
        if (request.Content is MultipartFormDataContent multipart)
        {
            foreach (var part in multipart)
            {
                var name = part.Headers.ContentDisposition?.Name?.Trim('"');
                var contentType = part.Headers.ContentType?.MediaType;
                var fileName = part.Headers.ContentDisposition?.FileName?.Trim('"');
                var body = await part.ReadAsStringAsync();
                parts.Add((name, contentType, fileName, body));
            }
        }
        else if (request.Content != null)
        {
            var body = await request.Content.ReadAsStringAsync();
            parts.Add((null, request.Content.Headers.ContentType?.MediaType, null, body));
        }
        CapturedContentParts.Add(parts);

        if (_responses.Count == 0)
            throw new InvalidOperationException("No more responses queued in MockHttpMessageHandler.");
        return _responses.Dequeue();
    }
}

/// <summary>
/// A testable subclass of TrueDeskApiService that allows injecting a custom HttpMessageHandler.
/// Uses reflection to replace the private _httpClient field with one backed by the mock handler.
/// </summary>
public class TestableTrueDeskApiService : TrueDeskApiService
{
    public TestableTrueDeskApiService(AppSettings settings, HttpMessageHandler handler)
        : base(settings)
    {
        // Replace the internally-created HttpClient with one using our mock handler
        var field = typeof(TrueDeskApiService).GetField("_httpClient",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(settings.ConnectionTimeoutSeconds)
        };
        field!.SetValue(this, httpClient);
    }
}

public class TrueDeskApiServiceTests : IDisposable
{
    private readonly AppSettings _settings;
    private readonly MockHttpMessageHandler _handler;
    private readonly TestableTrueDeskApiService _sut;

    public TrueDeskApiServiceTests()
    {
        Microsoft.Maui.Storage.SecureStorage.Clear();
        _settings = new AppSettings
        {
            ApiBaseUrl = "http://localhost:8118/api/v1",
            ConnectionTimeoutSeconds = 30
        };
        _handler = new MockHttpMessageHandler();
        _sut = new TestableTrueDeskApiService(_settings, _handler);
    }

    public void Dispose()
    {
        Microsoft.Maui.Storage.SecureStorage.Clear();
    }

    // ---------------------------------------------------------------
    // Helper
    // ---------------------------------------------------------------

    private static string LoginResponseJson(string token = "test-token", string userId = "user123", string fullname = "Test User")
    {
        return JsonSerializer.Serialize(new
        {
            accessToken = token,
            user = new { _id = userId, fullname }
        });
    }

    private string? GetLastRequestBody()
    {
        var lastReq = _handler.SentRequests.LastOrDefault();
        if (lastReq?.Content is StringContent sc)
            return sc.ReadAsStringAsync().GetAwaiter().GetResult();
        return null;
    }

    // ===============================================================
    // AuthenticateAsync
    // ===============================================================

    [Fact]
    public async Task AuthenticateAsync_ValidCredentials_ReturnsTrue()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, LoginResponseJson());

        var result = await _sut.AuthenticateAsync("admin", "password");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task AuthenticateAsync_ValidCredentials_SetsIsAuthenticated()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, LoginResponseJson());

        await _sut.AuthenticateAsync("admin", "password");

        _sut.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public async Task AuthenticateAsync_ValidCredentials_SetsCurrentUserId()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, LoginResponseJson(userId: "abc123"));

        await _sut.AuthenticateAsync("admin", "password");

        _sut.CurrentUserId.Should().Be("abc123");
    }

    [Fact]
    public async Task AuthenticateAsync_ValidCredentials_SetsCurrentUsername()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, LoginResponseJson());

        await _sut.AuthenticateAsync("admin", "password");

        _sut.CurrentUsername.Should().Be("admin");
    }

    [Fact]
    public async Task AuthenticateAsync_ValidCredentials_StoresTokenInSecureStorage()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, LoginResponseJson(token: "my-token"));

        await _sut.AuthenticateAsync("admin", "password");

        var storedToken = await Microsoft.Maui.Storage.SecureStorage.GetAsync("auth_token");
        storedToken.Should().Be("my-token");
    }

    [Fact]
    public async Task AuthenticateAsync_ValidCredentials_StoresUsernameInSecureStorage()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, LoginResponseJson());

        await _sut.AuthenticateAsync("admin", "password");

        var stored = await Microsoft.Maui.Storage.SecureStorage.GetAsync("auth_username");
        stored.Should().Be("admin");
    }

    [Fact]
    public async Task AuthenticateAsync_ValidCredentials_SendsCorrectPayload()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, LoginResponseJson());

        await _sut.AuthenticateAsync("myuser", "mypass");

        _handler.SentRequests.Should().ContainSingle();
        var req = _handler.SentRequests[0];
        req.Method.Should().Be(HttpMethod.Post);
        req.RequestUri!.ToString().Should().Be("http://localhost:8118/api/v1/login");
        var body = await req.Content!.ReadAsStringAsync();
        body.Should().Contain("\"username\":\"myuser\"");
        body.Should().Contain("\"password\":\"mypass\"");
    }

    [Fact]
    public async Task AuthenticateAsync_ValidCredentials_SetsAccessTokenHeader()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, LoginResponseJson(token: "header-token"));

        await _sut.AuthenticateAsync("admin", "password");

        // Verify token header is used on subsequent request
        _handler.EnqueueResponse(HttpStatusCode.OK, "[]");
        await _sut.GetTicketsAsync();

        var secondReq = _handler.SentRequests[1];
        secondReq.Headers.GetValues("accesstoken").Should().ContainSingle("header-token");
    }

    [Fact]
    public async Task AuthenticateAsync_InvalidCredentials_ReturnsFalse()
    {
        _handler.EnqueueResponse(HttpStatusCode.Unauthorized, "");

        var result = await _sut.AuthenticateAsync("admin", "wrong");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task AuthenticateAsync_InvalidCredentials_DoesNotSetAuthenticated()
    {
        _handler.EnqueueResponse(HttpStatusCode.Unauthorized, "");

        await _sut.AuthenticateAsync("admin", "wrong");

        _sut.IsAuthenticated.Should().BeFalse();
    }

    [Theory]
    [InlineData("", "password")]
    [InlineData("username", "")]
    [InlineData("", "")]
    [InlineData(null, "password")]
    [InlineData("username", null)]
    [InlineData("   ", "password")]
    [InlineData("username", "   ")]
    public async Task AuthenticateAsync_EmptyOrNullCredentials_ReturnsFalseWithoutHttpCall(string? username, string? password)
    {
        var result = await _sut.AuthenticateAsync(username!, password!);

        result.Should().BeFalse();
        _handler.SentRequests.Should().BeEmpty();
    }

    [Fact]
    public async Task AuthenticateAsync_NetworkError_ReturnsFalse()
    {
        // Enqueue a handler that throws
        var throwingHandler = new ThrowingHttpMessageHandler(new HttpRequestException("Network error"));
        var sut = new TestableTrueDeskApiService(_settings, throwingHandler);

        var result = await sut.AuthenticateAsync("admin", "password");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task AuthenticateAsync_Timeout_ReturnsFalse()
    {
        var throwingHandler = new ThrowingHttpMessageHandler(new TaskCanceledException("Timeout"));
        var sut = new TestableTrueDeskApiService(_settings, throwingHandler);

        var result = await sut.AuthenticateAsync("admin", "password");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task AuthenticateAsync_InvalidJsonResponse_ReturnsFalse()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, "not json at all");

        var result = await _sut.AuthenticateAsync("admin", "password");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task AuthenticateAsync_MissingAccessTokenInResponse_Throws()
    {
        // GetProperty("accessToken") throws KeyNotFoundException when the property is missing.
        // This is not caught by the service (only HttpRequestException, TaskCanceledException,
        // and JsonException are caught), so it propagates.
        _handler.EnqueueResponse(HttpStatusCode.OK, "{}");

        var act = () => _sut.AuthenticateAsync("admin", "password");

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task AuthenticateAsync_CalledTwice_UpdatesAccessTokenHeader()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, LoginResponseJson(token: "token1"));
        await _sut.AuthenticateAsync("admin", "pass1");

        _handler.EnqueueResponse(HttpStatusCode.OK, LoginResponseJson(token: "token2"));
        await _sut.AuthenticateAsync("admin2", "pass2");

        // Verify second token is active
        _handler.EnqueueResponse(HttpStatusCode.OK, "[]");
        await _sut.GetTicketsAsync();
        var req = _handler.SentRequests.Last();
        req.Headers.GetValues("accesstoken").Should().ContainSingle("token2");
    }

    // ===============================================================
    // TryRestoreSessionAsync
    // ===============================================================

    [Fact]
    public async Task TryRestoreSessionAsync_WithStoredToken_ReturnsTrue()
    {
        await Microsoft.Maui.Storage.SecureStorage.SetAsync("auth_token", "stored-token");
        await Microsoft.Maui.Storage.SecureStorage.SetAsync("auth_username", "storeduser");
        await Microsoft.Maui.Storage.SecureStorage.SetAsync("auth_userid", "storedid");

        var result = await _sut.TryRestoreSessionAsync();

        result.Should().BeTrue();
        _sut.IsAuthenticated.Should().BeTrue();
        _sut.CurrentUsername.Should().Be("storeduser");
        _sut.CurrentUserId.Should().Be("storedid");
    }

    [Fact]
    public async Task TryRestoreSessionAsync_NoStoredToken_ReturnsFalse()
    {
        var result = await _sut.TryRestoreSessionAsync();

        result.Should().BeFalse();
        _sut.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public async Task TryRestoreSessionAsync_EmptyStoredToken_ReturnsFalse()
    {
        await Microsoft.Maui.Storage.SecureStorage.SetAsync("auth_token", "");

        var result = await _sut.TryRestoreSessionAsync();

        result.Should().BeFalse();
    }

    [Fact]
    public async Task TryRestoreSessionAsync_SetsAccessTokenHeader()
    {
        await Microsoft.Maui.Storage.SecureStorage.SetAsync("auth_token", "restored-token");

        await _sut.TryRestoreSessionAsync();

        // Verify header is set on subsequent request
        _handler.EnqueueResponse(HttpStatusCode.OK, "[]");
        await _sut.GetTicketsAsync();
        var req = _handler.SentRequests.Last();
        req.Headers.GetValues("accesstoken").Should().ContainSingle("restored-token");
    }

    // ===============================================================
    // Logout
    // ===============================================================

    [Fact]
    public async Task Logout_ClearsAuthState()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, LoginResponseJson());
        await _sut.AuthenticateAsync("admin", "password");

        _handler.EnqueueResponse(HttpStatusCode.OK, "{}");
        await _sut.LogoutAsync();

        _sut.IsAuthenticated.Should().BeFalse();
        _sut.CurrentUsername.Should().BeNull();
        _sut.CurrentUserId.Should().BeNull();
    }

    [Fact]
    public async Task Logout_ClearsSecureStorage()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, LoginResponseJson());
        await _sut.AuthenticateAsync("admin", "password");

        _handler.EnqueueResponse(HttpStatusCode.OK, "{}");
        await _sut.LogoutAsync();

        var token = await Microsoft.Maui.Storage.SecureStorage.GetAsync("auth_token");
        token.Should().BeNull();
        var username = await Microsoft.Maui.Storage.SecureStorage.GetAsync("auth_username");
        username.Should().BeNull();
    }

    [Fact]
    public async Task Logout_WhenNotAuthenticated_DoesNotThrow()
    {
        var act = () => _sut.LogoutAsync();
        await act.Should().NotThrowAsync();
    }

    // ===============================================================
    // GetTicketsAsync
    // ===============================================================

    [Fact]
    public async Task GetTicketsAsync_SendsGetRequest()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, "{\"tickets\":[]}");

        await _sut.GetTicketsAsync();

        _handler.SentRequests.Should().ContainSingle();
        var req = _handler.SentRequests[0];
        req.Method.Should().Be(HttpMethod.Get);
        req.RequestUri!.ToString().Should().Be("http://localhost:8118/api/v1/tickets?limit=1000");
    }

    [Fact]
    public async Task GetTicketsAsync_ReturnsResponseContent()
    {
        var expected = "{\"tickets\":[{\"_id\":\"1\"}]}";
        _handler.EnqueueResponse(HttpStatusCode.OK, expected);

        var result = await _sut.GetTicketsAsync();

        result.Should().Be(expected);
    }

    [Fact]
    public async Task GetTicketsAsync_NonSuccessStatus_ThrowsHttpRequestException()
    {
        _handler.EnqueueResponse(HttpStatusCode.InternalServerError, "");

        var act = () => _sut.GetTicketsAsync();

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetTicketsAsync_IncludesAccessTokenHeader()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, LoginResponseJson(token: "my-token"));
        await _sut.AuthenticateAsync("admin", "pass");

        _handler.EnqueueResponse(HttpStatusCode.OK, "[]");
        await _sut.GetTicketsAsync();

        var req = _handler.SentRequests[1];
        req.Headers.GetValues("accesstoken").Should().ContainSingle("my-token");
    }

    // ===============================================================
    // GetTicketsPagedAsync
    // ===============================================================

    [Fact]
    public async Task GetTicketsPagedAsync_DefaultParams_CorrectUrl()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, "[]");

        await _sut.GetTicketsPagedAsync();

        var req = _handler.SentRequests[0];
        req.RequestUri!.ToString().Should().Be("http://localhost:8118/api/v1/tickets?page=0&limit=50");
    }

    [Fact]
    public async Task GetTicketsPagedAsync_CustomParams_CorrectUrl()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, "[]");

        await _sut.GetTicketsPagedAsync(page: 3, limit: 25);

        var req = _handler.SentRequests[0];
        req.RequestUri!.ToString().Should().Be("http://localhost:8118/api/v1/tickets?page=3&limit=25");
    }

    [Fact]
    public async Task GetTicketsPagedAsync_ReturnsResponseContent()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, "{\"data\":[]}");

        var result = await _sut.GetTicketsPagedAsync();

        result.Should().Be("{\"data\":[]}");
    }

    [Fact]
    public async Task GetTicketsPagedAsync_NonSuccess_Throws()
    {
        _handler.EnqueueResponse(HttpStatusCode.Forbidden, "");

        var act = () => _sut.GetTicketsPagedAsync();

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    // ===============================================================
    // SearchTicketsAsync
    // ===============================================================

    [Fact]
    public async Task SearchTicketsAsync_EncodesQuery()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, "[]");

        await _sut.SearchTicketsAsync("hello world");

        var req = _handler.SentRequests[0];
        // Uri.ToString() may decode %20 back to space; use OriginalString to verify encoding
        var uri = req.RequestUri!;
        uri.AbsolutePath.Should().Be("/api/v1/tickets/search");
        uri.Query.Should().Contain("search=");
        // The query value should represent "hello world" (either encoded or decoded)
        var query = Uri.UnescapeDataString(uri.Query);
        query.Should().Contain("search=hello world");
    }

    [Fact]
    public async Task SearchTicketsAsync_SpecialCharacters_AreEncoded()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, "[]");

        await _sut.SearchTicketsAsync("test&foo=bar");

        var req = _handler.SentRequests[0];
        // The special characters should be encoded so they don't break query parsing
        // Verify by checking the decoded query contains the original value
        var query = Uri.UnescapeDataString(req.RequestUri!.Query);
        query.Should().Contain("search=test&foo=bar");
    }

    [Fact]
    public async Task SearchTicketsAsync_ReturnsResponseContent()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, "{\"tickets\":[]}");

        var result = await _sut.SearchTicketsAsync("test");

        result.Should().Be("{\"tickets\":[]}");
    }

    [Fact]
    public async Task SearchTicketsAsync_NonSuccess_Throws()
    {
        _handler.EnqueueResponse(HttpStatusCode.BadRequest, "");

        var act = () => _sut.SearchTicketsAsync("test");

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    // ===============================================================
    // GetTicketAsync
    // ===============================================================

    [Fact]
    public async Task GetTicketAsync_CorrectUrl()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, "{\"ticket\":{}}");

        await _sut.GetTicketAsync("ticket123");

        var req = _handler.SentRequests[0];
        req.Method.Should().Be(HttpMethod.Get);
        req.RequestUri!.ToString().Should().Be("http://localhost:8118/api/v1/tickets/ticket123");
    }

    [Fact]
    public async Task GetTicketAsync_ReturnsResponseContent()
    {
        var expected = "{\"ticket\":{\"_id\":\"t1\"}}";
        _handler.EnqueueResponse(HttpStatusCode.OK, expected);

        var result = await _sut.GetTicketAsync("t1");

        result.Should().Be(expected);
    }

    // ===============================================================
    // GetStatusesAsync
    // ===============================================================

    [Fact]
    public async Task GetStatusesAsync_CorrectUrl()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, "[]");

        await _sut.GetStatusesAsync();

        _handler.SentRequests[0].RequestUri!.ToString()
            .Should().Be("http://localhost:8118/api/v1/tickets/status");
    }

    [Fact]
    public async Task GetStatusesAsync_NonSuccess_Throws()
    {
        _handler.EnqueueResponse(HttpStatusCode.InternalServerError, "");

        var act = () => _sut.GetStatusesAsync();

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    // ===============================================================
    // GetUsersAsync
    // ===============================================================

    [Fact]
    public async Task GetUsersAsync_CorrectUrl()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, "[]");

        await _sut.GetUsersAsync();

        _handler.SentRequests[0].RequestUri!.ToString()
            .Should().Be("http://localhost:8118/api/v1/users");
    }

    [Fact]
    public async Task GetUsersAsync_NonSuccess_Throws()
    {
        _handler.EnqueueResponse(HttpStatusCode.Unauthorized, "");

        var act = () => _sut.GetUsersAsync();

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    // ===============================================================
    // GetTicketTypesAsync
    // ===============================================================

    [Fact]
    public async Task GetTicketTypesAsync_CorrectUrl()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, "[]");

        await _sut.GetTicketTypesAsync();

        _handler.SentRequests[0].RequestUri!.ToString()
            .Should().Be("http://localhost:8118/api/v1/tickets/types");
    }

    [Fact]
    public async Task GetTicketTypesAsync_NonSuccess_Throws()
    {
        _handler.EnqueueResponse(HttpStatusCode.ServiceUnavailable, "");

        var act = () => _sut.GetTicketTypesAsync();

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    // ===============================================================
    // GetTagsAsync
    // ===============================================================

    [Fact]
    public async Task GetTagsAsync_CorrectUrl()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, "[]");

        await _sut.GetTagsAsync();

        _handler.SentRequests[0].RequestUri!.ToString()
            .Should().Be("http://localhost:8118/api/v1/tags/limit");
    }

    [Fact]
    public async Task GetTagsAsync_NonSuccess_Throws()
    {
        _handler.EnqueueResponse(HttpStatusCode.NotFound, "");

        var act = () => _sut.GetTagsAsync();

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    // ===============================================================
    // GetGroupsAsync
    // ===============================================================

    [Fact]
    public async Task GetGroupsAsync_CorrectUrl()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, "[]");

        await _sut.GetGroupsAsync();

        _handler.SentRequests[0].RequestUri!.ToString()
            .Should().Be("http://localhost:8118/api/v1/groups");
    }

    [Fact]
    public async Task GetGroupsAsync_NonSuccess_Throws()
    {
        _handler.EnqueueResponse(HttpStatusCode.Forbidden, "");

        var act = () => _sut.GetGroupsAsync();

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    // ===============================================================
    // AddCommentAsync
    // ===============================================================

    [Fact]
    public async Task AddCommentAsync_ValidInputs_ReturnsTrue()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, "");

        var result = await _sut.AddCommentAsync("ticket1", "owner1", "Hello");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task AddCommentAsync_SendsCorrectPayload()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, "");

        await _sut.AddCommentAsync("t1", "o1", "My comment");

        var req = _handler.SentRequests[0];
        req.Method.Should().Be(HttpMethod.Post);
        req.RequestUri!.ToString().Should().Be("http://localhost:8118/api/v1/tickets/addcomment");
        var body = await req.Content!.ReadAsStringAsync();
        body.Should().Contain("\"_id\":\"t1\"");
        body.Should().Contain("\"ownerId\":\"o1\"");
        body.Should().Contain("\"comment\":\"My comment\"");
    }

    [Fact]
    public async Task AddCommentAsync_ServerError_ReturnsFalse()
    {
        _handler.EnqueueResponse(HttpStatusCode.InternalServerError, "");

        var result = await _sut.AddCommentAsync("t1", "o1", "comment");

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("", "owner", "comment")]
    [InlineData(null, "owner", "comment")]
    [InlineData("   ", "owner", "comment")]
    [InlineData("id", "owner", "")]
    [InlineData("id", "owner", null)]
    [InlineData("id", "owner", "   ")]
    public async Task AddCommentAsync_EmptyIdOrComment_ReturnsFalseWithoutHttpCall(string? id, string owner, string? comment)
    {
        var result = await _sut.AddCommentAsync(id!, owner, comment!);

        result.Should().BeFalse();
        _handler.SentRequests.Should().BeEmpty();
    }

    // ===============================================================
    // AddNoteAsync
    // ===============================================================

    [Fact]
    public async Task AddNoteAsync_ValidInputs_ReturnsTrue()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, "");

        var result = await _sut.AddNoteAsync("ticket1", "owner1", "A note");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task AddNoteAsync_SendsCorrectPayload()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, "");

        await _sut.AddNoteAsync("t1", "o1", "My note");

        var req = _handler.SentRequests[0];
        req.Method.Should().Be(HttpMethod.Post);
        req.RequestUri!.ToString().Should().Be("http://localhost:8118/api/v1/tickets/addnote");
        var body = await req.Content!.ReadAsStringAsync();
        body.Should().Contain("\"ticketid\":\"t1\"");
        body.Should().Contain("\"owner\":\"o1\"");
        body.Should().Contain("\"note\":\"My note\"");
    }

    [Fact]
    public async Task AddNoteAsync_ServerError_ReturnsFalse()
    {
        _handler.EnqueueResponse(HttpStatusCode.BadRequest, "");

        var result = await _sut.AddNoteAsync("t1", "o1", "note");

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("", "owner", "note")]
    [InlineData(null, "owner", "note")]
    [InlineData("   ", "owner", "note")]
    [InlineData("id", "owner", "")]
    [InlineData("id", "owner", null)]
    [InlineData("id", "owner", "   ")]
    public async Task AddNoteAsync_EmptyTicketIdOrNote_ReturnsFalseWithoutHttpCall(string? ticketId, string owner, string? note)
    {
        var result = await _sut.AddNoteAsync(ticketId!, owner, note!);

        result.Should().BeFalse();
        _handler.SentRequests.Should().BeEmpty();
    }

    // ===============================================================
    // EditTicketAsync
    // ===============================================================

    [Fact]
    public async Task EditTicketAsync_ValidTicket_ReturnsTrue()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, "");

        var ticket = new Ticket
        {
            Id = "t1",
            Subject = "Updated",
            Issue = "Issue text",
            Priority = new Priority { Id = "p1" },
            Status = new Status { Id = "s1" }
        };

        var result = await _sut.EditTicketAsync(ticket);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task EditTicketAsync_SendsCorrectPayload()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, "");

        var ticket = new Ticket
        {
            Id = "t1",
            Subject = "Test Subject",
            Issue = "Test Issue",
            Priority = new Priority { Id = "p1" },
            Status = new Status { Id = "s1" }
        };

        await _sut.EditTicketAsync(ticket);

        var req = _handler.SentRequests[0];
        req.Method.Should().Be(HttpMethod.Put);
        req.RequestUri!.ToString().Should().Be("http://localhost:8118/api/v1/tickets/t1");
        var body = await req.Content!.ReadAsStringAsync();
        body.Should().Contain("\"subject\":\"Test Subject\"");
        body.Should().Contain("\"issue\":\"Test Issue\"");
        body.Should().Contain("\"priority\":\"p1\"");
        body.Should().Contain("\"status\":\"s1\"");
    }

    [Fact]
    public async Task EditTicketAsync_NullTicket_ReturnsFalse()
    {
        var result = await _sut.EditTicketAsync(null!);

        result.Should().BeFalse();
        _handler.SentRequests.Should().BeEmpty();
    }

    [Fact]
    public async Task EditTicketAsync_EmptyTicketId_ReturnsFalse()
    {
        var ticket = new Ticket { Id = "" };

        var result = await _sut.EditTicketAsync(ticket);

        result.Should().BeFalse();
        _handler.SentRequests.Should().BeEmpty();
    }

    [Fact]
    public async Task EditTicketAsync_WhitespaceTicketId_ReturnsFalse()
    {
        var ticket = new Ticket { Id = "   " };

        var result = await _sut.EditTicketAsync(ticket);

        result.Should().BeFalse();
        _handler.SentRequests.Should().BeEmpty();
    }

    [Fact]
    public async Task EditTicketAsync_ServerError_ReturnsFalse()
    {
        _handler.EnqueueResponse(HttpStatusCode.InternalServerError, "");

        var ticket = new Ticket { Id = "t1", Subject = "Sub" };

        var result = await _sut.EditTicketAsync(ticket);

        result.Should().BeFalse();
    }

    // ===============================================================
    // AssignTicketAsync
    // ===============================================================

    [Fact]
    public async Task AssignTicketAsync_Success_ReturnsTrue()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, "");

        var result = await _sut.AssignTicketAsync("t1", "u1");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task AssignTicketAsync_SendsCorrectRequest()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, "");

        await _sut.AssignTicketAsync("t1", "u1");

        var req = _handler.SentRequests[0];
        req.Method.Should().Be(HttpMethod.Put);
        req.RequestUri!.ToString().Should().Be("http://localhost:8118/api/v1/tickets/t1");
        var body = await req.Content!.ReadAsStringAsync();
        body.Should().Contain("\"assignee\":\"u1\"");
    }

    [Fact]
    public async Task AssignTicketAsync_ServerError_ReturnsFalse()
    {
        _handler.EnqueueResponse(HttpStatusCode.NotFound, "");

        var result = await _sut.AssignTicketAsync("t1", "u1");

        result.Should().BeFalse();
    }

    // ===============================================================
    // ClearTicketAssigneeAsync
    // ===============================================================

    [Fact]
    public async Task ClearTicketAssigneeAsync_Success_ReturnsTrue()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, "");

        var result = await _sut.ClearTicketAssigneeAsync("t1");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ClearTicketAssigneeAsync_CorrectUrl()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, "");

        await _sut.ClearTicketAssigneeAsync("t1");

        var req = _handler.SentRequests[0];
        req.Method.Should().Be(HttpMethod.Put);
        req.RequestUri!.ToString().Should().Be("http://localhost:8118/api/v1/tickets/t1");
        var body = await req.Content!.ReadAsStringAsync();
        body.Should().Contain("\"assignee\":null");
    }

    [Fact]
    public async Task ClearTicketAssigneeAsync_ServerError_ReturnsFalse()
    {
        _handler.EnqueueResponse(HttpStatusCode.BadRequest, "");

        var result = await _sut.ClearTicketAssigneeAsync("t1");

        result.Should().BeFalse();
    }

    // ===============================================================
    // UpdateTicketStatusAsync
    // ===============================================================

    [Fact]
    public async Task UpdateTicketStatusAsync_ValidInputs_ReturnsTrue()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, "");

        var result = await _sut.UpdateTicketStatusAsync("t1", "s1");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateTicketStatusAsync_SendsCorrectPayload()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, "");

        await _sut.UpdateTicketStatusAsync("t1", "s1");

        var req = _handler.SentRequests[0];
        req.Method.Should().Be(HttpMethod.Put);
        req.RequestUri!.ToString().Should().Be("http://localhost:8118/api/v1/tickets/t1");
        var body = await req.Content!.ReadAsStringAsync();
        body.Should().Contain("\"status\":\"s1\"");
    }

    [Theory]
    [InlineData("", "s1")]
    [InlineData(null, "s1")]
    [InlineData("   ", "s1")]
    [InlineData("t1", "")]
    [InlineData("t1", null)]
    [InlineData("t1", "   ")]
    public async Task UpdateTicketStatusAsync_EmptyInputs_ReturnsFalseWithoutHttpCall(string? ticketId, string? statusId)
    {
        var result = await _sut.UpdateTicketStatusAsync(ticketId!, statusId!);

        result.Should().BeFalse();
        _handler.SentRequests.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateTicketStatusAsync_ServerError_ReturnsFalse()
    {
        _handler.EnqueueResponse(HttpStatusCode.InternalServerError, "");

        var result = await _sut.UpdateTicketStatusAsync("t1", "s1");

        result.Should().BeFalse();
    }

    // ===============================================================
    // CreateTicketAsync
    // ===============================================================

    [Fact]
    public async Task CreateTicketAsync_ValidSubject_ReturnsTrue()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, "");

        var result = await _sut.CreateTicketAsync("Test ticket", null, null, null, null, null);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CreateTicketAsync_SendsCorrectPayload()
    {
        // Set CurrentUserId via authentication first
        _handler.EnqueueResponse(HttpStatusCode.OK, LoginResponseJson(userId: "owner123"));
        await _sut.AuthenticateAsync("admin", "pass");

        _handler.EnqueueResponse(HttpStatusCode.OK, "");

        await _sut.CreateTicketAsync("My Subject", "Some issue", "type1", "pri1", "group1", "assign1");

        var req = _handler.SentRequests[1]; // second request (first is auth)
        req.Method.Should().Be(HttpMethod.Post);
        req.RequestUri!.ToString().Should().Be("http://localhost:8118/api/v1/tickets/create");
        var body = await req.Content!.ReadAsStringAsync();
        body.Should().Contain("\"subject\":\"My Subject\"");
        body.Should().Contain("\"issue\":\"Some issue\"");
        body.Should().Contain("\"owner\":\"owner123\"");
        body.Should().Contain("\"type\":\"type1\"");
        body.Should().Contain("\"priority\":\"pri1\"");
        body.Should().Contain("\"group\":\"group1\"");
        body.Should().Contain("\"assignee\":\"assign1\"");
    }

    [Fact]
    public async Task CreateTicketAsync_NullOptionalFields_OmitsThemFromPayload()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, "");

        await _sut.CreateTicketAsync("Subject only", null, null, null, null, null);

        var body = await _handler.SentRequests[0].Content!.ReadAsStringAsync();
        body.Should().Contain("\"subject\":\"Subject only\"");
        body.Should().Contain("\"issue\":\"\""); // null issue becomes empty string
        body.Should().NotContain("\"type\"");
        body.Should().NotContain("\"priority\"");
        body.Should().NotContain("\"group\"");
        body.Should().NotContain("\"assignee\"");
    }

    [Fact]
    public async Task CreateTicketAsync_EmptySubject_ReturnsFalse()
    {
        var result = await _sut.CreateTicketAsync("", null, null, null, null, null);

        result.Should().BeFalse();
        _handler.SentRequests.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateTicketAsync_WhitespaceSubject_ReturnsFalse()
    {
        var result = await _sut.CreateTicketAsync("   ", null, null, null, null, null);

        result.Should().BeFalse();
        _handler.SentRequests.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateTicketAsync_ServerError_SetsLastError()
    {
        _handler.EnqueueResponse(HttpStatusCode.BadRequest, "Validation failed");

        var result = await _sut.CreateTicketAsync("Subject", null, null, null, null, null);

        result.Should().BeFalse();
        _sut.LastError.Should().Contain("400");
        _sut.LastError.Should().Contain("Validation failed");
    }

    [Fact]
    public async Task CreateTicketAsync_Success_DoesNotSetLastError()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, "");

        await _sut.CreateTicketAsync("Subject", null, null, null, null, null);

        // LastError should not be set on success (it remains whatever it was before)
        _sut.LastError.Should().BeNull();
    }

    // ===============================================================
    // AddTicketAsync (legacy method)
    // ===============================================================

    [Fact]
    public async Task AddTicketAsync_ValidTitle_SendsCorrectRequest()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, "{\"ticket\":{}}");

        await _sut.AddTicketAsync("My Title", "Description", "507f1f77bcf86cd799439011");

        var req = _handler.SentRequests[0];
        req.Method.Should().Be(HttpMethod.Post);
        req.RequestUri!.ToString().Should().Be("http://localhost:8118/api/v1/tickets/create");
        var body = await req.Content!.ReadAsStringAsync();
        body.Should().Contain("\"subject\":\"My Title\"");
        body.Should().Contain("\"issue\":\"Description\"");
        body.Should().Contain("\"assignee\":\"507f1f77bcf86cd799439011\"");
    }

    [Fact]
    public async Task AddTicketAsync_EmptyTitle_ThrowsArgumentException()
    {
        var act = () => _sut.AddTicketAsync("", "desc", "someId");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task AddTicketAsync_WhitespaceTitle_ThrowsArgumentException()
    {
        var act = () => _sut.AddTicketAsync("   ", "desc", "someId");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task AddTicketAsync_NonSuccess_Throws()
    {
        _handler.EnqueueResponse(HttpStatusCode.BadRequest, "");

        var act = () => _sut.AddTicketAsync("Title", "desc", "someId");

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    // ===============================================================
    // UploadAttachmentAsync
    // ===============================================================

    [Fact]
    public async Task UploadAttachmentAsync_Success_ReturnsTrue()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, "");

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("file content"));
        var result = await _sut.UploadAttachmentAsync("t1", stream, "test.pdf");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task UploadAttachmentAsync_SendsMultipartFormData()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, "");

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("data"));
        await _sut.UploadAttachmentAsync("t1", stream, "document.pdf");

        var req = _handler.SentRequests[0];
        req.Method.Should().Be(HttpMethod.Post);
        req.RequestUri!.ToString().Should().Be("http://localhost:8118/tickets/uploadattachment");
        // Verify multipart content was captured with file and ticketId parts
        var parts = _handler.CapturedContentParts[0];
        parts.Should().Contain(p => p.FileName == "document.pdf");
        parts.Should().Contain(p => p.Name == "ticketId" && p.Body == "t1");
    }

    [Fact]
    public async Task UploadAttachmentAsync_ServerError_ReturnsFalse()
    {
        _handler.EnqueueResponse(HttpStatusCode.InternalServerError, "");

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("data"));
        var result = await _sut.UploadAttachmentAsync("t1", stream, "file.txt");

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("test.pdf", "application/pdf")]
    [InlineData("image.png", "image/png")]
    [InlineData("photo.jpg", "image/jpeg")]
    [InlineData("photo.jpeg", "image/jpeg")]
    [InlineData("animation.gif", "image/gif")]
    [InlineData("word.doc", "application/msword")]
    [InlineData("word.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData("sheet.xls", "application/vnd.ms-excel")]
    [InlineData("sheet.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [InlineData("readme.txt", "text/plain")]
    [InlineData("archive.zip", "application/zip")]
    [InlineData("unknown.xyz", "application/octet-stream")]
    [InlineData("noext", "application/octet-stream")]
    public async Task UploadAttachmentAsync_CorrectMimeType(string fileName, string expectedMimeType)
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, "");

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("data"));
        await _sut.UploadAttachmentAsync("t1", stream, fileName);

        // Use captured parts since MultipartFormDataContent is disposed after the method returns
        var capturedParts = _handler.CapturedContentParts[0];
        var filePart = capturedParts.First(p => p.FileName != null);
        filePart.ContentType.Should().Be(expectedMimeType);
    }

    // ===============================================================
    // DownloadAttachmentAsync
    // ===============================================================

    [Fact]
    public async Task DownloadAttachmentAsync_Success_ReturnsStream()
    {
        var responseMsg = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(Encoding.UTF8.GetBytes("file bytes"))
        };
        _handler.EnqueueResponse(responseMsg);

        var result = await _sut.DownloadAttachmentAsync("/uploads/file.pdf");

        result.Should().NotBeNull();
        using var reader = new StreamReader(result!);
        var content = await reader.ReadToEndAsync();
        content.Should().Be("file bytes");
    }

    [Fact]
    public async Task DownloadAttachmentAsync_CorrectUrl()
    {
        _handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(Array.Empty<byte>())
        });

        await _sut.DownloadAttachmentAsync("/uploads/file.pdf");

        var req = _handler.SentRequests[0];
        req.Method.Should().Be(HttpMethod.Get);
        // Base URL without /api/v1
        req.RequestUri!.ToString().Should().Be("http://localhost:8118/uploads/file.pdf");
    }

    [Fact]
    public async Task DownloadAttachmentAsync_NotFound_ReturnsNull()
    {
        _handler.EnqueueResponse(HttpStatusCode.NotFound, "");

        var result = await _sut.DownloadAttachmentAsync("/uploads/missing.pdf");

        result.Should().BeNull();
    }

    // ===============================================================
    // DeleteAttachmentAsync
    // ===============================================================

    [Fact]
    public async Task DeleteAttachmentAsync_Success_ReturnsTrue()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, "");

        var result = await _sut.DeleteAttachmentAsync("t1", "att1");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAttachmentAsync_CorrectUrl()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, "");

        await _sut.DeleteAttachmentAsync("t1", "att1");

        var req = _handler.SentRequests[0];
        req.Method.Should().Be(HttpMethod.Delete);
        req.RequestUri!.ToString().Should().Be("http://localhost:8118/api/v1/tickets/t1/attachments/remove/att1");
    }

    [Fact]
    public async Task DeleteAttachmentAsync_NotFound_ReturnsFalse()
    {
        _handler.EnqueueResponse(HttpStatusCode.NotFound, "");

        var result = await _sut.DeleteAttachmentAsync("t1", "att1");

        result.Should().BeFalse();
    }

    // ===============================================================
    // GetAttachmentUrl
    // ===============================================================

    [Fact]
    public void GetAttachmentUrl_ConstructsCorrectUrl()
    {
        var url = _sut.GetAttachmentUrl("/uploads/tickets/abc/file.pdf");

        url.Should().Be("http://localhost:8118/uploads/tickets/abc/file.pdf");
    }

    [Fact]
    public void GetAttachmentUrl_StripsApiV1FromBase()
    {
        var url = _sut.GetAttachmentUrl("/some/path");

        url.Should().NotContain("/api/v1");
        url.Should().Be("http://localhost:8118/some/path");
    }

    // ===============================================================
    // IsAuthenticated property
    // ===============================================================

    [Fact]
    public void IsAuthenticated_Initially_IsFalse()
    {
        _sut.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public async Task IsAuthenticated_AfterLogin_IsTrue()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, LoginResponseJson());
        await _sut.AuthenticateAsync("admin", "pass");

        _sut.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public async Task IsAuthenticated_AfterLogout_IsFalse()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, LoginResponseJson());
        await _sut.AuthenticateAsync("admin", "pass");

        _handler.EnqueueResponse(HttpStatusCode.OK, "{}");
        await _sut.LogoutAsync();

        _sut.IsAuthenticated.Should().BeFalse();
    }
}

/// <summary>
/// An HttpMessageHandler that throws a specified exception on every request.
/// Useful for simulating network errors and timeouts.
/// </summary>
public class ThrowingHttpMessageHandler : HttpMessageHandler
{
    private readonly Exception _exception;

    public ThrowingHttpMessageHandler(Exception exception)
    {
        _exception = exception;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        throw _exception;
    }
}
