using PMSIntegration.Application.Logging;
using PMSIntegration.Application.PMSInterfaces;
using PMSIntegration.Infrastructure.Services;

namespace PMSIntegration.Worker.Workers
{
    public class PatientWorker : BackgroundService
    {
        private readonly FileLogger _logger;
        private readonly IStartupService _startupService;
        private readonly IServiceProvider _serviceProvider;

        public PatientWorker(FileLogger logger, IStartupService startupService, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _startupService = startupService;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInfo("PMSIntegration Service starting...");
                _startupService.InitializeDatabase();
                _logger.LogInfo("Database initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during startup.");
                return;
            }
            
            var firstRun = true;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (firstRun)
                    {
                        _logger.LogInfo("Starting initial patient export cycle...");
                        firstRun = false;
                    }
                    else
                    {
                        _logger.LogInfo($"Starting patient export cycle at: {DateTimeOffset.UtcNow}");
                    }

                    using var scope = _serviceProvider.CreateScope();
                    var exportService = scope.ServiceProvider.GetRequiredService<PatientExportService>();

                    await exportService.ExportPatientsAsync();

                    _logger.LogInfo($"Patient export cycle completed successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during patient export cycle");
                }

                // wait 60 minutes before next cycle
                var delayMinutes = GetConfiguredDelay();
                _logger.LogInfo($"Waiting {delayMinutes} minutes until next cycle...");
                await Task.Delay(TimeSpan.FromMinutes(delayMinutes), stoppingToken);
            }
            _logger.LogInfo("Service stopped");
        }

        private int GetConfiguredDelay()
        {
            return 60;
        }
    }
}
