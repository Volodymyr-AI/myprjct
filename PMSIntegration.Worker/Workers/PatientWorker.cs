using PMSIntegration.Application.Logging;
using PMSIntegration.Application.PMSInterfaces;
using PMSIntegration.Core.Configuration.Abstract;
using PMSIntegration.Infrastructure.Database;
using PMSIntegration.Infrastructure.Services;

namespace PMSIntegration.Worker.Workers
{
    public class PatientWorker : BackgroundService
    {
        private readonly FileLogger _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IntegrationConfig _config;
        private readonly int _intervalMinutes;

        public PatientWorker(
            FileLogger logger, 
            IServiceProvider serviceProvider,
            IntegrationConfig config)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _config = config;
            _intervalMinutes = 60; // Default 60 minutes, could be made configurable
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInfo($"Patient Worker started. Will sync every {_intervalMinutes} minutes");
            
            // Initial delay to let service fully start
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            
            var isFirstRun = true;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInfo(isFirstRun 
                        ? "Starting initial patient synchronization..." 
                        : $"Starting patient synchronization at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    
                    isFirstRun = false;

                    using var scope = _serviceProvider.CreateScope();
                    var exportService = scope.ServiceProvider.GetRequiredService<PatientExportService>();

                    // Export patients and insurance
                    await exportService.ExportPatientsAsync();
                    
                    // Save last sync time
                    using var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
                    await db.Config.SetAsync("LastSyncTime", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
                    
                    _logger.LogInfo("Patient synchronization completed successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during patient synchronization");
                }

                // Wait for next cycle
                try
                {
                    _logger.LogDebug($"Next synchronization in {_intervalMinutes} minutes");
                    await Task.Delay(TimeSpan.FromMinutes(_intervalMinutes), stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // Service is stopping
                    break;
                }
            }
            
            _logger.LogInfo("Patient Worker stopped");
        }
    }
}
