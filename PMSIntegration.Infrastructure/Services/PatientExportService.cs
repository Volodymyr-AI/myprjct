using System.Data.SQLite;
using Microsoft.Extensions.DependencyInjection;
using PMSIntegration.Application.Hub;
using PMSIntegration.Application.Logging;
using PMSIntegration.Core.Configuration.Abstract;
using PMSIntegration.Core.Configuration.Enum;
using PMSIntegration.Core.Patients.Models;
using PMSIntegration.Infrastructure.Database.Config;
using PMSIntegration.Infrastructure.Database.Insurance;
using PMSIntegration.Infrastructure.OpenDental.Services;

namespace PMSIntegration.Infrastructure.Services;

public class PatientExportService
{
    private readonly IntegrationConfig _config;
    private readonly FileLogger _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _databasePath;
    
    public PatientExportService(
        IntegrationConfig config,
        FileLogger logger,
        IServiceProvider serviceProvider)
    {
        _config = config;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _databasePath = ServiceHub.GetDatabasePath();
    }
    
    public async Task ExportPatientsAsync()
    {
        try
        {
            _logger.LogInfo("Starting patient export process");

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
                    _logger.LogError(new InvalidOperationException($"Unknown provider: {_config.Provider}"), 
                        "Unknown PMS provider configured");
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during patient export process");
            throw;
        }
    }
    
    private async Task ExportFromOpenDentalAsync()
    {
        try
        {
            var openDentalService = _serviceProvider.GetService<OpenDentalService>();
            
            if (openDentalService == null)
            {
                _logger.LogError(
                    new InvalidOperationException("OpenDental service not configured"), 
                    "OpenDental service is required for OpenDental provider");
                return;
            }

            _logger.LogInfo($"Exporting patients from OpenDental since {_config.ExportStartDate:yyyy-MM-dd}");

            // Get patients from OpenDental API
            var openDentalPatients = await openDentalService.ExportPatientsAsync(_config.ExportStartDate.DateTime);
            
            if (!openDentalPatients.Any())
            {
                _logger.LogInfo("No new patients found in OpenDental");
                return;
            }

            _logger.LogInfo($"Retrieved {openDentalPatients.Count} patients from OpenDental API");

            // Get existing patients from database
            var existingPatientIds = await GetExistingPatientIdsAsync();

            // Filter new patients
            var newPatients = openDentalPatients
                .Where(p => !existingPatientIds.Contains(p.Id.Value))
                .ToList();
            
            if (!newPatients.Any())
            {
                _logger.LogInfo("All retrieved patients already exist in local database");
                return;
            }

            _logger.LogInfo($"Found {newPatients.Count} new patients to save");

            // Save new patients
            var savedCount = await SavePatientsToDatabase(newPatients);
            _logger.LogInfo($"Successfully saved {savedCount} new patients to database");
            await ExportInsuranceDataAsync(openDentalService);
            _logger.LogInfo($"Insurance data exported to database");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during OpenDental patient export");
            throw;
        }
    }
    
    private async Task<HashSet<int>> GetExistingPatientIdsAsync()
    {
        var existingIds = new HashSet<int>();
        
        try
        {
            using var connection = new SQLiteConnection($"Data Source={_databasePath};Version=3;");
            await connection.OpenAsync();

            const string sql = "SELECT Id FROM Patients";
            using var command = new SQLiteCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                existingIds.Add(reader.GetInt32(0));
            }

            _logger.LogDebug($"Found {existingIds.Count} existing patients in database");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading existing patient IDs from database");
        }

        return existingIds;
    }

    private async Task<int> SavePatientsToDatabase(List<Core.Patients.Models.Patient> patients)
    {
        var savedCount = 0;
        
        try
        {
            using var connection = new SQLiteConnection($"Data Source={_databasePath};Version=3;");
            await connection.OpenAsync();

            const string insertSql = @"
                INSERT INTO Patients (
                    Id, FirstName, LastName, Phone, Email, 
                    Address, City, State, ZipCode, BirthDate, DataExported
                )
                VALUES (
                    @id, @firstName, @lastName, @phone, @email, 
                    @address, @city, @state, @zipCode, @birthDate, @dataExported
                )";
            
            using var transaction = connection.BeginTransaction();

            foreach (var patient in patients)
            {
                try
                {
                    using var command = new SQLiteCommand(insertSql, connection, transaction);
                    
                    command.Parameters.AddWithValue("@id", patient.Id.Value);
                    command.Parameters.AddWithValue("@firstName", patient.FirstName);
                    command.Parameters.AddWithValue("@lastName", patient.LastName);
                    command.Parameters.AddWithValue("@phone", patient.Phone);
                    command.Parameters.AddWithValue("@email", patient.Email ?? string.Empty);
                    command.Parameters.AddWithValue("@address", patient.Address);
                    command.Parameters.AddWithValue("@city", patient.City);
                    command.Parameters.AddWithValue("@state", patient.State);
                    command.Parameters.AddWithValue("@zipCode", patient.ZipCode);
                    command.Parameters.AddWithValue("@birthDate", patient.DateOfBirth.ToString("yyyy-MM-dd"));
                    command.Parameters.AddWithValue("@dataExported", patient.ReportReady ? 1 : 0);

                    await command.ExecuteNonQueryAsync();
                    savedCount++;
                    
                    if (savedCount % 10 == 0)
                    {
                        _logger.LogInfo($"Progress: Saved {savedCount}/{patients.Count} patients");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, 
                        $"Failed to save patient {patient.FirstName} {patient.LastName} (ID: {patient.Id})");
                }
            }
            await transaction.CommitAsync();
            
            await UpdateLastExportDate(connection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving patients to database");
        }

        return savedCount;
    }
    
    private async Task UpdateLastExportDate(SQLiteConnection connection)
    {
        try
        {
            var lastExportDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            ConfigTable.InsertOrUpdate(connection, "LastExportDate", lastExportDate);
            _logger.LogDebug($"Updated LastExportDate to {lastExportDate}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating last export date");
        }
    }
    
    // ======== INSURANCE ========
    private async Task ExportInsuranceDataAsync(OpenDentalService openDentalService)
    {
        try
        {
            _logger.LogInfo("Starting insurance data export for existing patients");
            
            // Get all patient IDs from database
            var patientIds = await GetAllPatientIdsAsync();
            
            if (!patientIds.Any())
            {
                _logger.LogInfo("No patients found for insurance export");
                return;
            }
            
            _logger.LogInfo($"Exporting insurance data for {patientIds.Count} patients");
            
            // Export insurance data for all patients
            var allInsurance = await openDentalService.ExportAllPatientsInsuranceAsync(patientIds);
            
            if (allInsurance.Any())
            {
                var savedCount = await SaveInsuranceToDatabase(allInsurance);
                _logger.LogInfo($"Successfully saved {savedCount} insurance records");
            }
            else
            {
                _logger.LogInfo("No insurance data found for any patients");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during insurance data export");
        }
    }
    
    private async Task<List<int>> GetAllPatientIdsAsync()
    {
        var patientIds = new List<int>();
        
        try
        {
            using var connection = new SQLiteConnection($"Data Source={_databasePath};Version=3;");
            await connection.OpenAsync();
    
            const string sql = "SELECT Id FROM Patients ORDER BY Id";
            using var command = new SQLiteCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();
    
            while (await reader.ReadAsync())
            {
                patientIds.Add(reader.GetInt32(0));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading patient IDs from database");
        }
    
        return patientIds;
    }
    
    private async Task<int> SaveInsuranceToDatabase(List<PatientInsurance> insuranceList)
    {
        var savedCount = 0;
        
        try
        {
            using var connection = new SQLiteConnection($"Data Source={_databasePath};Version=3;");
            await connection.OpenAsync();
            
            using var transaction = connection.BeginTransaction();
    
            foreach (var insurance in insuranceList)
            {
                try
                {
                    await InsuranceTable.SaveInsuranceAsync(connection, insurance);
                    savedCount++;
                    
                    if (savedCount % 10 == 0)
                    {
                        _logger.LogInfo($"Progress: Saved {savedCount}/{insuranceList.Count} insurance records");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, 
                        $"Failed to save insurance for patient {insurance.PatientId} - {insurance.CarrierName}");
                }
            }
            
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving insurance to database");
        }
    
        return savedCount;
    }
}