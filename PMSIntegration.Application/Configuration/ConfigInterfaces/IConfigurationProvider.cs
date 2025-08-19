using PMSIntegration.Core.Configuration.Abstract;

namespace PMSIntegration.Application.Configuration.ConfigInterfaces;

public interface IConfigurationProvider
{
    IntegrationConfig LoadConfig();
}