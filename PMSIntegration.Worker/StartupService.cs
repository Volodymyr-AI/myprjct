using PMSIntegration.Application.Logging;
using PMSIntegration.Application.PMSInterfaces;
using PMSIntegration.Core.Configuration.Abstract;
using PMSIntegration.Infrastructure.Database;

namespace PMSIntegration.Worker;

public class StartupService : IStartupService
{
    private readonly IntegrationConfig _config;
    private readonly FileLogger _logger;
    public StartupService(IntegrationConfig config, FileLogger logger)
    {
        _config = config;
        _logger = logger;
    }

    public void InitializeDatabase()
    {
        // Initialize database
        var dbInitializer = new DatabaseInitializer(_logger);
        dbInitializer.Initialize();
        
        // Save configuration
        SaveConfigurationToDatabase();
    }
    
    private void SaveConfigurationToDatabase()
    {
        using var dbContext = new DatabaseContext(_logger);
        
        // Save config values
        dbContext.Config.SetAsync("Provider", _config.Provider.ToString()).Wait();
        dbContext.Config.SetAsync("ExportStartDate", _config.ExportStartDate.ToString("yyyy-MM-dd")).Wait();
        dbContext.Config.SetAsync("LastInitialized", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")).Wait();
        dbContext.Config.SetAsync("Version", "1.0.0").Wait();
        
        _logger.LogDebug("Configuration saved to database");
    }
}