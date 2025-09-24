using PMSIntegration.Application.Hub;
using PMSIntegration.Application.Logging;
using PMSIntegration.Infrastructure.Services;

namespace PMSIntegration.Worker.Workers;

/*
public class ReportWorker : BackgroundService
{
    
    private readonly FileLogger _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _reportsPath;
    private FileSystemWatcher? _fileWatcher;
    private readonly HashSet<string> _recentlyProcessed = new();
    private readonly object _recentlyProcessedLock = new();
    
    public ReportWorker(FileLogger logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _reportsPath = ServiceHub.GetReportsPath();
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInfo("Report Worker starting...");
        _logger.LogInfo($"Monitoring folder: {_reportsPath}");
        
        try
        {
            await ProcessExistingReports();
            
            SetupFileWatcher();
            
            // Check queue every 30 seconds
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                
                ClearRecentlyProcessed();
                
                await CheckForMissedReports();
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInfo("Report Worker stopping due to cancellation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Report Worker");
        }
        finally
        {
            _fileWatcher?.Dispose();
            _logger.LogInfo("Report Worker stopped");
        }
    }
    
    private void SetupFileWatcher()
    {
        try
        {
            _fileWatcher = new FileSystemWatcher(_reportsPath)
            {
                Filter = "*.pdf",
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime | NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };
            
            _fileWatcher.Created += async (sender, e) => await OnReportDetected(e.FullPath);
            
            _logger.LogInfo($"FileSystemWatcher configured for: {_reportsPath}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting up FileSystemWatcher");
        }
    }
    
    private async Task OnReportDetected(string filePath)
    {
        try
        {
            // Check if file was processed
            lock (_recentlyProcessedLock)
            {
                if (_recentlyProcessed.Contains(filePath))
                {
                    _logger.LogDebug($"File recently processed, skipping: {Path.GetFileName(filePath)}");
                    return;
                }

                _recentlyProcessed.Add(filePath);
            }

            // Wait until file is fully written
            if (!await WaitForFileReady(filePath))
            {
                _logger.LogWarn($"File not ready after timeout: {filePath}");
                return;
            }

            _logger.LogInfo($"New report detected: {Path.GetFileName(filePath)}");

            // Add to queueu
            using var scope = _serviceProvider.CreateScope();
            var uploadService = scope.ServiceProvider.GetRequiredService<ReportUploadService>();

            uploadService.EnqueueReport(filePath);

            
            await uploadService.ProcessQueueAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing detected report: {filePath}");
        }
    }
    
    private void ClearRecentlyProcessed()
    {
        lock (_recentlyProcessedLock)
        {
            _recentlyProcessed.Clear();
        }
    }
    
    private async Task ProcessExistingReports()
    {
        try
        {
            var existingReports = Directory.GetFiles(_reportsPath, "*.pdf")
                .OrderBy(f => File.GetCreationTime(f))
                .ToArray();
            
            if (existingReports.Length == 0)
            {
                _logger.LogInfo("No existing reports found in Reports folder");
                return;
            }
            
            _logger.LogInfo($"Found {existingReports.Length} existing reports to process");
            
            using var scope = _serviceProvider.CreateScope();
            var uploadService = scope.ServiceProvider.GetRequiredService<ReportUploadService>();
            
            foreach (var reportPath in existingReports)
            {
                uploadService.EnqueueReport(reportPath);
            }
            
            await uploadService.ProcessQueueAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing existing reports");
        }
    }
    
    private async Task CheckForMissedReports()
    {
        try
        {
            var currentFiles = Directory.GetFiles(_reportsPath, "*.pdf");
            
            if (currentFiles.Length > 0)
            {
                using var scope = _serviceProvider.CreateScope();
                var uploadService = scope.ServiceProvider.GetRequiredService<ReportUploadService>();
                
                foreach (var file in currentFiles)
                {
                    uploadService.EnqueueReport(file);
                }
                
                await uploadService.ProcessQueueAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for missed reports");
        }
    }
    
    private async Task<bool> WaitForFileReady(string filePath, int maxWaitSeconds = 10)
    {
        var startTime = DateTime.Now;
        
        while ((DateTime.Now - startTime).TotalSeconds < maxWaitSeconds)
        {
            try
            {
                using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    return true;
                }
            }
            catch (IOException)
            {
                await Task.Delay(500);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error waiting for file: {filePath}");
                return false;
            }
        }
        
        return false;
    }
    
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInfo("Report Worker stop requested");
        
        if (_fileWatcher != null)
        {
            _fileWatcher.EnableRaisingEvents = false;
        }
        
        await base.StopAsync(cancellationToken);
    }
}
*/