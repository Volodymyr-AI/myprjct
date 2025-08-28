namespace PMSIntegration.Application.Configuration.Interface;

public interface IAppConfigurationService
{
    string Get(string section, string key);
}