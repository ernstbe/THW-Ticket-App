namespace THWTicketApp.Services;

/// <summary>
/// Application settings and configuration.
/// API URL should be configured via environment or app settings in production.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// The base URL for the TrueDesk API.
    /// Default is localhost for development. Configure for production deployment.
    /// </summary>
    public string ApiBaseUrl { get; set; } = "http://localhost:8118/api/v1";

    /// <summary>
    /// Connection timeout in seconds for API requests.
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 30;
}
