using System.Data.SQLite;
using PMSIntegration.Application.Interfaces;
using PMSIntegration.Core.Configuration.Abstract;
using PMSIntegration.Infrastructure.Database;
using PMSIntegration.Infrastructure.Database.Config;

namespace PMSIntegration.Worker;

public class StartupService : IStartupService
{
    private readonly IntegrationConfig _config;

    public StartupService(IntegrationConfig config)
    {
        _config = config;
    }

    public void Initialize()
    {
        var projectRoot = GetProjectRootPath();
        var dbFullPath = Path.Combine(projectRoot, _config.DbPath);
        
        DatabaseInitializer.Initialize(dbFullPath);

        using var connection = new SQLiteConnection($"Data Source={dbFullPath};Version=3;");
        connection.Open();

        foreach (var prop in _config.GetType().GetProperties())
        {
            var key = prop.Name;
            var value = prop.GetValue(_config)?.ToString() ?? "";
            ConfigTable.InsertOrUpdate(connection, key, value);
        }
    }

    private static string GetProjectRootPath()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        
        if(File.Exists(Path.Combine(currentDirectory, "PMSIntegration.Worker.csproj")))
        {
            return currentDirectory;
        }
        
        var assemblyLocation = AppContext.BaseDirectory;
        var directory = new DirectoryInfo(assemblyLocation);

        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "PMSIntegration.Worker.csproj")))
        {
            directory = directory.Parent;
        }
        
        return directory?.FullName ?? currentDirectory;
    }
}