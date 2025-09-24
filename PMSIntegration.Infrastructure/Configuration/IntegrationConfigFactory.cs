using PMSIntegration.Application.Configuration.Interface;
using PMSIntegration.Core.Configuration.Abstract;
using PMSIntegration.Core.Configuration.Enum;
using PMSIntegration.Core.Configuration.Models;

namespace PMSIntegration.Infrastructure.Configuration;

public static class IntegrationConfigFactory
{
    public static IntegrationConfig Create(IAppConfigurationService configService)
    {
        var provider = Enum.Parse<PmsProvider>(
            configService.Get("Integration", "Provider"), 
            ignoreCase: true);
        
        var exportStartDate = DateTimeOffset.Parse(
            configService.Get("Integration", "ExportStartDate"));

        return provider switch
        {
            PmsProvider.OpenDental => new OpenDentalIntegrationConfig
            {
                Provider = provider,
                ExportStartDate = exportStartDate,
                AuthScheme = configService.Get("OpenDental", "AuthScheme"),
                AuthToken = configService.Get("OpenDental", "AuthToken"),
                ApiBaseUrl = configService.Get("OpenDental", "ApiBaseUrl"),
                TimeoutSeconds = int.Parse(configService.Get("OpenDental", "TimeoutSeconds")),
                OpenDentalImagePath = configService.Get("OpenDental", "OpenDentalImagePath"),
            },
            
            PmsProvider.Dentrix => throw new NotImplementedException(
                "Dentrix provider is not implemented yet"),
                
            PmsProvider.EagleSoft => throw new NotImplementedException(
                "EagleSoft provider is not implemented yet"),
                
            _ => throw new NotSupportedException(
                $"Provider {provider} is not supported")
        };
    }
}