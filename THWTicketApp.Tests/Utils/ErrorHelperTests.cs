using System.Net;
using FluentAssertions;
using THWTicketApp.Utils;
using Xunit;

namespace THWTicketApp.Tests.Utils;

public class ErrorHelperTests
{
    [Fact]
    public void Categorize_TaskCanceledException_ReturnsTimeout()
    {
        var ex = new TaskCanceledException();
        var (message, canRetry) = ErrorHelper.Categorize(ex);

        message.Should().Contain("Zeitüberschreitung");
        canRetry.Should().BeTrue();
    }

    [Fact]
    public void Categorize_HttpRequestException_ReturnsConnectionError()
    {
        var ex = new HttpRequestException("Connection refused");
        var (message, canRetry) = ErrorHelper.Categorize(ex);

        message.Should().Contain("Verbindung");
        canRetry.Should().BeTrue();
    }

    [Fact]
    public void Categorize_HttpRequestException_Unauthorized_ReturnsSessionExpired()
    {
        var ex = new HttpRequestException("Unauthorized", null, HttpStatusCode.Unauthorized);
        var (message, canRetry) = ErrorHelper.Categorize(ex);

        message.Should().Contain("Sitzung abgelaufen");
        canRetry.Should().BeFalse();
    }

    [Fact]
    public void Categorize_HttpRequestException_Forbidden_ReturnsNoPermission()
    {
        var ex = new HttpRequestException("Forbidden", null, HttpStatusCode.Forbidden);
        var (message, canRetry) = ErrorHelper.Categorize(ex);

        message.Should().Contain("Berechtigung");
        canRetry.Should().BeFalse();
    }

    [Fact]
    public void Categorize_HttpRequestException_NotFound_ReturnsNotFound()
    {
        var ex = new HttpRequestException("Not Found", null, HttpStatusCode.NotFound);
        var (message, canRetry) = ErrorHelper.Categorize(ex);

        message.Should().Contain("nicht gefunden");
        canRetry.Should().BeFalse();
    }

    [Fact]
    public void Categorize_HttpRequestException_ServerError_ReturnsRetryable()
    {
        var ex = new HttpRequestException("Server Error", null, HttpStatusCode.InternalServerError);
        var (message, canRetry) = ErrorHelper.Categorize(ex);

        message.Should().Contain("Serverfehler");
        canRetry.Should().BeTrue();
    }

    [Fact]
    public void Categorize_JsonException_ReturnsInvalidFormat()
    {
        var ex = new System.Text.Json.JsonException("Bad JSON");
        var (message, canRetry) = ErrorHelper.Categorize(ex);

        message.Should().Contain("Datenformat");
        canRetry.Should().BeFalse();
    }

    [Fact]
    public void Categorize_GenericException_ReturnsGenericMessage()
    {
        var ex = new InvalidOperationException("test");
        var (message, canRetry) = ErrorHelper.Categorize(ex);

        message.Should().NotBeNullOrEmpty();
        canRetry.Should().BeTrue();
    }
}
