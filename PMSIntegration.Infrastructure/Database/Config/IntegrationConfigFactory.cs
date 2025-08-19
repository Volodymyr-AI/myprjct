using PMSIntegration.Application.Configuration.Interface;
using PMSIntegration.Core.Configuration.Abstract;
using PMSIntegration.Core.Configuration.Enum;
using PMSIntegration.Core.Configuration.Models;

namespace PMSIntegration.Infrastructure.Database.Config;

public static class IntegrationConfigFactory
{
    public static IntegrationConfig Create(IAppConfigurationService configService)
    {
        var provider = Enum.Parse<PmsProvider>(configService.Get("Integration", "Provider"), ignoreCase: true);
        
        var exportStartDate = DateTimeOffset.Parse(
            configService.Get("Integration", "ExportStartDate"));

        var dbPath = configService.Get("Integration", "DbPath");

        return provider switch
        {
            PmsProvider.OpenDental => new OpenDentalIntegrationConfig
            {
                Provider = provider,
                ExportStartDate = exportStartDate,
                DbPath = dbPath,
                AuthScheme = configService.Get("OpenDental", "AuthScheme"),
                AuthToken = configService.Get("OpenDental", "AuthToken")
            },
            _ => throw new NotImplementedException($"Provider {provider} not implemented yet")
        };
    }
}