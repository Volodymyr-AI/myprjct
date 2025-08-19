using System.Data.SQLite;
using PMSIntegration.Application.Interfaces;
using PMSIntegration.Core.Configuration.Abstract;
using PMSIntegration.Infrastructure.Database;
using PMSIntegration.Infrastructure.Database.Config;

namespace PMSIntegration.Worker;

public class StartupService : IStartupService
{
    private readonly IntegrationConfig _config;
    private readonly string _databasePath;

    public StartupService(IntegrationConfig config, string databasePath)
    {
        _config = config;
        _databasePath = databasePath;
    }

    public void InitializeDatabase()
    {
        DatabaseInitializer.Initialize(_databasePath);

        using var connection = new SQLiteConnection($"Data Source={_databasePath};Version=3;");
        connection.Open();

        foreach (var prop in _config.GetType().GetProperties())
        {
            var key = prop.Name;
            var value = prop.GetValue(_config)?.ToString() ?? "";
            ConfigTable.InsertOrUpdate(connection, key, value);
        }
    }
}