using PMSIntegration.Application.Configuration.Interface;

namespace PMSIntegration.Application.Configuration;

public class IniConfigurationService : IAppConfigurationService
{
    private readonly Dictionary<string, Dictionary<string, string>> _data;

    public IniConfigurationService(string path)
    {
        _data = IniParser.Parse(path);
    }

    public string Get(string section, string key)
    {
        return _data.TryGetValue(section, out var sectionData) &&
               sectionData.TryGetValue(key, out var value)
            ? value
            : throw new Exception($"Missing configuration: [{section}] {key}");
    }
}