using System.Runtime.InteropServices;

namespace PMSIntegration.Application.Configuration;

public static class IniParser
{
    public static Dictionary<string, Dictionary<string, string>> Parse(string iniFilePath)
    {
        var data = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        string? currentSection = null;

        foreach (var rawLine in File.ReadAllLines(iniFilePath))
        {
            var line = rawLine.Trim();
            
            if(string.IsNullOrWhiteSpace(line) || line.StartsWith(";") || line.StartsWith("#"))
                continue;

            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                currentSection = line[1..^1].Trim();
                if (!data.ContainsKey(currentSection))
                    data[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            else if (line.Contains('=') && currentSection != null)
            {
                var split = line.Split('=',2);
                var key = split[0].Trim();
                var value = split[1].Trim();
                data[currentSection][key] = value;
            }
        }
        
        return data;
    }
}