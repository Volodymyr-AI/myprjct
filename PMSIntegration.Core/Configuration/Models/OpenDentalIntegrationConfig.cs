using PMSIntegration.Core.Configuration.Abstract;

namespace PMSIntegration.Core.Configuration.Models;

public class OpenDentalIntegrationConfig : IntegrationConfig
{
    public string AuthScheme { get; set; } = "";
    public string AuthToken { get; set; } = "";
    public string ApiBaseUrl { get; set; } = "http://localhost:30222";
    public int TimeoutSeconds { get; set; } = 30;
    public string OpenDentalImagePath  { get; set; }
}