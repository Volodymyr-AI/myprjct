using System.Data;
using System.Data.SQLite;
using PMSIntegration.Application.Logging;
using PMSIntegration.Core.Configuration.Abstract;
using PMSIntegration.Core.Configuration.Enum;
using PMSIntegration.Infrastructure.OpenDental.Services;

namespace PMSIntegration.Infrastructure.Services;

public class PatientExportService
{
    private readonly IntegrationConfig _config;
    private readonly FileLogger _logger;
    private readonly OpenDentalService _openDentalService;
    
    public PatientExportService(
        IntegrationConfig config,
        FileLogger logger,
        OpenDentalService? openDentalService = null)
    {
        _config = config;
        _logger = logger;
        _openDentalService = openDentalService;
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
        if (_openDentalService == null)
        {
            _logger.LogError(new InvalidOperationException("OpenDental service not configured"), 
                "OpenDental service is required for OpenDental provider");
            return;
        }

        try
        {
            _logger.LogInfo($"Exporting patients from OpenDental since {_config.ExportStartDate:yyyy-MM-dd}");

            // Get patients from OpenDental API
            var openDentalPatients = await _openDentalService.ExportPatientsAsync(_config.ExportStartDate.DateTime);
            
            if (!openDentalPatients.Any())
            {
                _logger.LogInfo("No new patients found in OpenDental");
                return;
            }

            _logger.LogInfo($"Retrieved {openDentalPatients.Count} patients from OpenDental API");

            // Get existing patients from database
            var existingPatientIds = await GetExistingPatientIdsAsync();

            // Filter new patients
            var newPatients = openDentalPatients.Where(p => !existingPatientIds.Contains(p.Id.Value)).ToList();
            
            if (!newPatients.Any())
            {
                _logger.LogInfo("All retrieved patients already exist in local database");
                return;
            }

            _logger.LogInfo($"Found {newPatients.Count} new patients to save");

            // Save new patients
            var savedCount = await SavePatientsToDatabase(newPatients);
            _logger.LogInfo($"Successfully saved {savedCount} new patients to database");
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
            var dbPath = Path.Combine(GetProjectRootPath(), _config.DbPath);
            using var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;");
            await connection.OpenAsync();

            const string sql = "SELECT Id FROM Patients";
            using var command = new SQLiteCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                existingIds.Add(reader.GetInt32("Id"));
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
            var dbPath = Path.Combine(GetProjectRootPath(), _config.DbPath);
            using var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;");
            await connection.OpenAsync();

            const string insertSql = @"
                INSERT INTO Patients (Id, FirstName, LastName, Phone, Email, Address, City, State, ZipCode, BirthDate, DataExported)
                VALUES (@id, @firstName, @lastName, @phone, @email, @address, @city, @state, @zipCode, @birthDate, @dataExported)";

            foreach (var patient in patients)
            {
                try
                {
                    using var command = new SQLiteCommand(insertSql, connection);
                    command.Parameters.AddWithValue("@id", patient.Id.Value);
                    command.Parameters.AddWithValue("@firstName", patient.FirstName);
                    command.Parameters.AddWithValue("@lastName", patient.LastName);
                    command.Parameters.AddWithValue("@phone", patient.Phone);
                    command.Parameters.AddWithValue("@email", patient.Email);
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
                        _logger.LogInfo($"Saved {savedCount}/{patients.Count} patients");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to save patient {patient.FirstName} {patient.LastName} (ID: {patient.Id})");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving patients to database");
        }

        return savedCount;
    }
    
    private static string GetProjectRootPath()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        
        if (File.Exists(Path.Combine(currentDirectory, "PMSIntegration.Worker.csproj")))
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