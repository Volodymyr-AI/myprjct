using System.Data.SQLite;
using PMSIntegration.Application.Hub;
using PMSIntegration.Application.Logging;
using PMSIntegration.Core.Configuration.Abstract;
using PMSIntegration.Core.Configuration.Enum;
using PMSIntegration.Core.Configuration.Models;

using Path = System.IO.Path;

namespace PMSIntegration.Infrastructure.Services;

public class ReportUploadService
{
    /*
    private readonly IntegrationConfig _config;
    private readonly FileLogger _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _dbPath;
    private readonly Queue<string> _processingQueue;
    private readonly HashSet<string> _processingFiles;
    private readonly object _queueLock = new();
    private bool _isProcessing = false;
    private readonly CleanupService _cleanupService;
    
    public ReportUploadService(
        IntegrationConfig config, 
        FileLogger logger,
        IServiceProvider serviceProvider,
        CleanupService cleanupService)
    {
        _config = config;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _dbPath = ServiceHub.GetDatabasePath();
        _processingQueue = new Queue<string>();
        _processingFiles = new HashSet<string>();
        _cleanupService = cleanupService;
    }

    public void EnqueueReport(string reportFilePath)
    {
        lock (_queueLock)
        {
            if (!_processingQueue.Contains(reportFilePath) && !_processingFiles.Contains(reportFilePath))
            {
                _processingQueue.Enqueue(reportFilePath);
                _logger.LogDebug($"Report added to queue: {reportFilePath}. Queue size: {_processingQueue.Count}");
            }
        }
    }

    // Main queue for report importing
    public async Task ProcessQueueAsync()
    {
        if (_isProcessing)
        {
            _logger.LogDebug("Report already processing");
            return;
        }
        
        _isProcessing = true;

        try
        {
            await _cleanupService.Cleanup(_dbPath);

            while (true)
            {
                string? reportPath = null;
                lock (_queueLock)
                {
                    if (_processingQueue.Count == 0)
                        break;

                    reportPath = _processingQueue.Dequeue();
                    _processingFiles.Add(reportPath);
                }

                if (!File.Exists(reportPath))
                {
                    _logger.LogWarn($"Report file not found, skipping: {reportPath}");
                    RemoveFromProcessing(reportPath);
                    continue;
                }

                await ProcessingSingleReport(reportPath);
                RemoveFromProcessing(reportPath);

                // Small delay between file processing
                await Task.Delay(500);
            }
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private void RemoveFromProcessing(string filePath)
    {
        lock (_queueLock)
        {
            _processingFiles.Remove(filePath);
        }
    }

    // Process a single report through all stages
    private async Task ProcessingSingleReport(string reportPath)
    {
        int reportId = 0;

        try
        {
            _logger.LogDebug($"Processing report: {reportPath}");

            // Stage 1: Create a record in db after report uploaded 
            reportId = await CreateReportRecord(reportPath);
            if (reportId == 0)
            {
                _logger.LogError(new Exception("Failed to create report record"),
                    $"Could not save report to database: {reportPath}");
                return;
            }

            //Stage 2: Take patients data for further need ( patient name )
            var patientName = ExtractPatientNameFromFileName(reportPath);
            if (string.IsNullOrEmpty(patientName))
            {
                await MarkReportAsFailed(reportId, "Could not extract patient name from file name");
                _logger.LogError(new Exception("Invalid file name"), 
                    $"Could not extract patient name from: {Path.GetFileName(reportPath)}");
                return;
            }
            
            await MarkReportAsProcessed(reportId, patientName);
            _logger.LogDebug($"Extracted patient name from file name: {patientName}");

            //Stage 3: IMPORTED - Copy into PMS
            var destinationPath = await ImportToPms(reportPath, patientName);
            if (string.IsNullOrEmpty(destinationPath))
            {
                await MarkReportAsFailed(reportId, "Failed to import to PMS - folder not found");
                return;
            }
            
            await MarkReportAsImported(reportId, destinationPath);
            _logger.LogInfo($"Report imported to: {destinationPath}");

            // Stage 4: SUCCESS - Delete from Reports
            try
            {
                File.Delete(reportPath);
                await MarkReportAsSuccess(reportId);
                _logger.LogDebug($"Report successfully processed and deleted: {reportPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to delete report file: {reportPath}");
                await MarkReportAsSuccess(reportId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing report: {reportPath}");
            
            if (reportId > 0)
            {
                await MarkReportAsFailed(reportId, ex.Message);
            }
        }
    }

    // ======== REPORT ========
    
    /// <summary>
    /// // Extract patient name from file name
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    private string? ExtractPatientNameFromFileName(string filePath)
    {
        try
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            
            var cleanName = fileName
                .Replace("DentalRay_Report_", "")
                .Replace("DentalRay_", "")
                .Replace("Report_", "")
                .Replace("_", " ")
                .Trim();
            
            if (string.IsNullOrWhiteSpace(cleanName))
            {
                _logger.LogWarn($"Could not extract patient name from file: {fileName}");
                return null;
            }
            
            return cleanName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting patient name from file name");
            return null;
        }
    }

    /// <summary>
    /// Add a record of a new uploaded report to db
    /// </summary>
    /// <param name="reportPath"></param>
    /// <returns></returns>
    private async Task<int> CreateReportRecord(string reportPath)
    {
        try
        {
            using var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;");
            await connection.OpenAsync();
            
            return await ReportTable.InsertUploadedReport(
                connection,
                Path.GetFileName(reportPath),
                reportPath
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating report record in database");
            return 0;
        }
    }
    
    // ======== IMPORT LOGIC ===========
    
    /// <summary>
    /// Central API for logic of reports importing depending on a pms
    /// </summary>
    /// <param name="reportFilePath"></param>
    /// <param name="patientName"></param>
    /// <returns></returns>
    private async Task<string?> ImportToPms(string reportFilePath, string patientName)
    {
        try
        {
            return _config.Provider switch
            {
                PmsProvider.OpenDental => await ImportToOpenDental(reportFilePath, patientName),
                PmsProvider.Dentrix => await ImportToDentrix(reportFilePath, patientName),
                PmsProvider.EagleSoft => await ImportToEagleSoft(reportFilePath, patientName),
                _ => null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error importing report to {_config.Provider}");
            return null;
        }
    }
    
    /// <summary>
    /// OpenDental report import logic
    /// </summary>
    /// <param name="reportFilePath"></param>
    /// <param name="patientName"></param>
    /// <returns></returns>
    private async Task<string?> ImportToOpenDental(string reportFilePath, string patientName)
    {
        try
        {
            if (_config is not OpenDentalIntegrationConfig openDentalConfig)
            {
                _logger.LogError(new InvalidOperationException("OpenDental configuration not found"), 
                    "OpenDental config missing");
                return null;
            }
            
            if (!Directory.Exists(openDentalConfig.OpenDentalImagePath))
            {
                _logger.LogError(new DirectoryNotFoundException(
                    $"OpenDental images folder not found: {openDentalConfig.OpenDentalImagePath}"), 
                    "OpenDental folder missing");
                return null;
            }
            
            // Search for patient folder
            var patientFolder = await FindPatientFolder(openDentalConfig.OpenDentalImagePath, patientName);
            
            if (string.IsNullOrEmpty(patientFolder))
            {
                _logger.LogWarn($"Patient folder not found for: {patientName}");
                
                // If folder not found - create it 
                var firstLetter = GetFirstLetterFromName(patientName);
                var folderName = patientName.Replace(" ", "");
                
                var targetDirectory = Path.Combine(
                    openDentalConfig.OpenDentalImagePath,
                    firstLetter,
                    folderName
                );
                
                Directory.CreateDirectory(targetDirectory);
                _logger.LogInfo($"Created new patient folder: {targetDirectory}");
                
                patientFolder = targetDirectory;
            }
            
            // Copy report into found folder
            var targetFilePath = Path.Combine(
                patientFolder,
                $"Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
            );
            
            File.Copy(reportFilePath, targetFilePath, overwrite: true);
            
            return targetFilePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing to OpenDental");
            return null;
        }
    }
    
    private async Task<string?> FindPatientFolder(string openDentalPath, string patientName)
    {
        try
        {
            // Split name into parts
            var nameParts = patientName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (nameParts.Length < 2)
            {
                _logger.LogWarn($"Invalid patient name format: {patientName}");
                if (nameParts.Length == 1)
                {
                    return await FindFolderBySingleName(openDentalPath, nameParts[0]);
                }
                return null;
            }
            
            var firstName = nameParts[0];
            var lastName = nameParts[nameParts.Length - 1];
            
            // Take first letter of surname
            var firstLetter = lastName[0].ToString().ToUpper();
            var letterPath = Path.Combine(openDentalPath, firstLetter);
            
            if (!Directory.Exists(letterPath))
            {
                _logger.LogDebug($"Letter folder does not exist: {letterPath}");
                return null;
            }
            
            // Search for a folder that starts with LastnameFirstname
            var searchPatterns = new[]
            {
                $"{lastName}{firstName}*",        // AllowedAllen*
                $"{firstName}{lastName}*",        // AllenAllowed*  
                $"{lastName}*",                   // Allowed*
                $"*{lastName}*",                  // *Allowed*
                $"*{firstName}*"                  // *Allen*
            };
            foreach (var pattern in searchPatterns)
            {
                _logger.LogDebug($"Searching for pattern: {pattern} in {letterPath}");
                var directories = Directory.GetDirectories(letterPath, pattern, SearchOption.TopDirectoryOnly);
            
                if (directories.Length > 0)
                {
                    // If found more than 1 folder try to get best match
                    var bestMatch = FindBestMatch(directories, firstName, lastName);
                    if (!string.IsNullOrEmpty(bestMatch))
                    {
                        _logger.LogInfo($"Found existing patient folder: {bestMatch}");
                        return bestMatch;
                    }
                }
            }
        
            _logger.LogDebug($"No folder found for patient: {patientName}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error finding patient folder for: {patientName}");
            return null;
        }
    }
    private async Task<string?> FindFolderBySingleName(string openDentalPath, string name)
    {
        try
        {
            var firstLetter = name[0].ToString().ToUpper();
            var letterPath = Path.Combine(openDentalPath, firstLetter);
        
            if (!Directory.Exists(letterPath))
            {
                return null;
            }
            
            var directories = Directory.GetDirectories(letterPath, $"*{name}*", SearchOption.TopDirectoryOnly);
        
            if (directories.Length > 0)
            {
                _logger.LogInfo($"Found folder for single name '{name}': {directories[0]}");
                return directories[0];
            }
        
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error finding folder by single name: {name}");
            return null;
        }
    }

    private string? FindBestMatch(string[] directories, string firstName, string lastName)
    {
        foreach (var dir in directories)
        {
            var folderName = Path.GetFileName(dir);
            
            if (folderName.Contains(firstName, StringComparison.OrdinalIgnoreCase) && 
                folderName.Contains(lastName, StringComparison.OrdinalIgnoreCase))
            {
                return dir;
            }
        }
        
        return directories.Length > 0 ? directories[0] : null;
    }
    
    private string GetFirstLetterFromName(string patientName)
    {
        var nameParts = patientName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lastName = nameParts.Length > 1 ? nameParts[nameParts.Length - 1] : nameParts[0];
        return lastName[0].ToString().ToUpper();
    }
    
    // TODO: Import to Dentrix
    private async Task<string?> ImportToDentrix(string reportFilePath, string patientName)
    {
        _logger.LogWarn("Dentrix import not implemented yet");
        return null;
    }
    // TODO: Import to EagleSoft
    private async Task<string?> ImportToEagleSoft(string reportFilePath, string patientName)
    {
        _logger.LogWarn("EagleSoft import not implemented yet");
        return null;
    }
    
    // ======== STATUS MARKING ========
    private async Task MarkReportAsProcessed(int reportId, string patientName)
    {
        try
        {
            using var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;");
            await connection.OpenAsync();
            await ReportTable.UpdateToProcessed(connection, reportId, patientName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating report {reportId} to PROCESSED");
        }
    }
    
    private async Task MarkReportAsImported(int reportId, string destinationPath)
    {
        try
        {
            using var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;");
            await connection.OpenAsync();
            await ReportTable.UpdateToImported(connection, reportId, destinationPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating report {reportId} to IMPORTED");
        }
    }
    private async Task MarkReportAsSuccess(int reportId)
    {
        try
        {
            using var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;");
            await connection.OpenAsync();
            await ReportTable.UpdateToSuccess(connection, reportId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating report {reportId} to SUCCESS");
        }
    }
    
    private async Task MarkReportAsFailed(int reportId, string errorMessage)
    {
        try
        {
            using var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;");
            await connection.OpenAsync();
            await ReportTable.UpdateToFailed(connection, reportId, errorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating report {reportId} to FAILED");
        }
    } */
}