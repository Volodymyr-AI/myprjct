using System.Data.SQLite;
using Microsoft.Extensions.DependencyInjection;
using PMSIntegration.Application.Hub;
using PMSIntegration.Application.Logging;
using PMSIntegration.Core.Configuration.Abstract;
using PMSIntegration.Core.Configuration.Enum;
using PMSIntegration.Core.Patients.Models;
using PMSIntegration.Infrastructure.Database;
using PMSIntegration.Infrastructure.OpenDental.DTO;
using PMSIntegration.Infrastructure.OpenDental.Services;

namespace PMSIntegration.Infrastructure.Services;

public class PatientExportService
{
    private readonly IntegrationConfig _config;
    private readonly FileLogger _logger;
    private readonly IServiceProvider _serviceProvider;
    
    public PatientExportService(
        IntegrationConfig config,
        FileLogger logger,
        IServiceProvider serviceProvider)
    {
        _config = config;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }
    
    /// <summary>
    /// Main export method - routes to appropriate PMS provider
    /// </summary>
    public async Task ExportPatientsAsync()
    {
        try
        {
            _logger.LogInfo($"Starting patient export for provider: {_config.Provider}");

            switch (_config.Provider)
            {
                case PmsProvider.OpenDental:
                    await ExportFromOpenDentalAsync();
                    break;
                    
                case PmsProvider.Dentrix:
                    _logger.LogWarn("Dentrix integration not implemented yet");
                    break;
                    
                case PmsProvider.EagleSoft:
                    _logger.LogWarn("EagleSoft integration not implemented yet");
                    break;
                    
                default:
                    _logger.LogError(
                        new NotSupportedException($"Unknown provider: {_config.Provider}"), 
                        "Unknown PMS provider configured");
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during patient export process");
            // Don't rethrow - let the service continue running
        }
    }
    
    private async Task ExportFromOpenDentalAsync()
    {
        OpenDentalService openDentalService = null;

        try
        {
            openDentalService = _serviceProvider.GetService<OpenDentalService>();
            if (openDentalService == null)
            {
                _logger.LogError(
                    new InvalidOperationException("OpenDental service not configured"), 
                    "OpenDental service is required for OpenDental provider");
                return;
            }

            if (!await openDentalService.IsApiAvailable())
            {
                _logger.LogWarn("OpenDental API is not available. Skipping export.");
                return;
            }
            
            // Get last export date from config
            var lastExportDate = await GetLastExportDate();
            var exportFromDate = lastExportDate ?? _config.ExportStartDate.DateTime;
            
            _logger.LogDebug($"Exporting patients from OpenDental since {exportFromDate:yyyy-MM-dd}");

            // Get patients from API
            var apiPatients = await openDentalService.ExportPatientsAsync(exportFromDate);
            
            if (!apiPatients.Any())
            {
                _logger.LogInfo("No patients found in OpenDental API");
                return;
            }

            using var db = new DatabaseContext(_logger);

            var existingIds = await db.Patients.GetAllIdsAsync();
            _logger.LogDebug($"Found {existingIds.Count} existing patients in database");
            
            // Filter new patients only
            var newPatients = apiPatients
                .Where(p => !existingIds.Contains(p.Id))
                .ToList();

            if (!newPatients.Any())
            {
                _logger.LogInfo("No new patients");
                return;
            }
            
            _logger.LogInfo($"Found {newPatients.Count} new patients to import");

            // Save new patients in transaction
            var savedCount = await db.Patients.BulkSaveAsync(newPatients);
            _logger.LogInfo($"Successfully saved {savedCount} new patients to database");
            
            // Export insurance for new patients only
            if (savedCount > 0)
            {
                var newPatientIds = newPatients.Select(p => p.Id).ToList();
                await ExportInsuranceForPatientsAsync(openDentalService, db, newPatientIds);
            }
            
            // Update last export date
            await db.Config.SetAsync("LastExportDate", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
            await db.Config.SetAsync("LastPatientCount", savedCount.ToString());
            
            _logger.LogInfo($"Patient export completed. Imported {savedCount} patients");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error connecting to OpenDental API");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "OpenDental API request timed out");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during OpenDental export");
        }
        finally
        {
            openDentalService?.Dispose();
        }
    }
    
    /// <summary>
    /// Get last export date from database config
    /// </summary>
    private async Task<DateTime?> GetLastExportDate()
    {
        try
        {
            using var db = new DatabaseContext(_logger);
            var dateStr = await db.Config.GetAsync("LastExportDate");
            
            if (!string.IsNullOrEmpty(dateStr) && DateTime.TryParse(dateStr, out var date))
            {
                _logger.LogDebug($"Last export date from config: {date:yyyy-MM-dd HH:mm:ss}");
                return date;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading last export date from config");
        }
        
        return null;
    }
    
    // ======== INSURANCE ========
    /// <summary>
    /// Export insurance data for specific patients
    /// </summary>
    private async Task ExportInsuranceForPatientsAsync(
        OpenDentalService openDentalService,
        DatabaseContext db,
        List<int> patientIds)
    {
        try
        {
            if (!patientIds.Any())
            {
                _logger.LogDebug("No patients for insurance export");
                return;
            }
            
            _logger.LogInfo($"Exporting insurance data for {patientIds.Count} new patients");
            
            var allInsurance = new List<Insurance>();
            var failedCount = 0;
            
            // Process in batches to avoid overwhelming the API
            const int batchSize = 10;
            for (int i = 0; i < patientIds.Count; i += batchSize)
            {
                var batch = patientIds.Skip(i).Take(batchSize).ToList();
                
                foreach (var patientId in batch)
                {
                    try
                    {
                        // Get insurance data from API
                        var insuranceData = await openDentalService.ExportPatientInsuranceAsync(patientId);
                        
                        if (insuranceData.Any())
                        {
                            // Convert to generic insurance model
                            var genericInsurance = insuranceData
                                .Select(dto => dto.ToBillingInsurance())
                                .ToList();
                            
                            allInsurance.AddRange(genericInsurance);
                            _logger.LogDebug($"Found {insuranceData.Count} insurance plans for patient {patientId}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to export insurance for patient {patientId}");
                        failedCount++;
                    }
                }
                
                // Rate limiting between batches
                if (i + batchSize < patientIds.Count)
                {
                    await Task.Delay(500); // 500ms delay between batches
                }
                
                _logger.LogInfo($"Insurance export progress: {Math.Min(i + batchSize, patientIds.Count)}/{patientIds.Count}");
            }
            
            // Save all insurance records
            if (allInsurance.Any())
            {
                var savedCount = await db.Insurance.BulkSaveAsync(allInsurance);
                _logger.LogInfo($"Successfully saved {savedCount} insurance records");
                
                if (failedCount > 0)
                {
                    _logger.LogWarn($"Failed to export insurance for {failedCount} patients");
                }
            }
            else
            {
                _logger.LogInfo("No insurance data found for new patients");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during insurance data export");
            // Don't rethrow - continue with patient data even if insurance fails
        }
    }
}