using System.Data.SQLite;
using PMSIntegration.Application.Hub;
using PMSIntegration.Application.PMSInterfaces;
using PMSIntegration.Core.Configuration.Abstract;
using PMSIntegration.Core.Configuration.Models;
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

    public void InitializeDatabase()
    {
        var databasePath = ServiceHub.GetDatabasePath();
        
        DatabaseInitializer.Initialize(databasePath);
        
        SaveConfigurationToDatabase(databasePath);
    }
    
    private void SaveConfigurationToDatabase(string databasePath)
    {
        using var connection = new SQLiteConnection($"Data Source={databasePath};Version=3;");
        connection.Open();
        
        ConfigTable.InsertOrUpdate(connection, "Provider", _config.Provider.ToString());
        ConfigTable.InsertOrUpdate(connection, "ExportStartDate", _config.ExportStartDate.ToString("yyyy-MM-dd"));
        
        if (_config is OpenDentalIntegrationConfig openDentalConfig)
        {
            ConfigTable.InsertOrUpdate(connection, "AuthScheme", openDentalConfig.AuthScheme);
            ConfigTable.InsertOrUpdate(connection, "AuthToken", openDentalConfig.AuthToken);
            ConfigTable.InsertOrUpdate(connection, "ApiBaseUrl", openDentalConfig.ApiBaseUrl);
            ConfigTable.InsertOrUpdate(connection, "TimeoutSeconds", openDentalConfig.TimeoutSeconds.ToString());
        }
        
        ConfigTable.InsertOrUpdate(connection, "LastInitialized", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
        ConfigTable.InsertOrUpdate(connection, "Version", "1.0.0");
    }
}