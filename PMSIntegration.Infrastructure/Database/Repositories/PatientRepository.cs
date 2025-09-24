using System.Data.SQLite;
using PMSIntegration.Application.Logging;
using PMSIntegration.Core.Patients.Models;

namespace PMSIntegration.Infrastructure.Database.Repositories;

public class PatientRepository
{
    private readonly DatabaseContext _context;
    private readonly FileLogger _logger;
    
    public PatientRepository(DatabaseContext context, FileLogger logger)
    {
        _context = context;
        _logger = logger;
    }
    
    /// <summary>
    /// Get all patient IDs
    /// </summary>
    public async Task<HashSet<int>> GetAllIdsAsync()
    {
        var ids = new HashSet<int>();
        const string sql = "SELECT Id FROM Patients";
        
        using var command = new SQLiteCommand(sql, _context.Connection);
        using var reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            ids.Add(reader.GetInt32(0));
        }
        
        return ids;
    }
    
    /// <summary>
    /// Save multiple patients
    /// </summary>
    public async Task<int> BulkSaveAsync(List<Patient> patients)
    {
        const string sql = """
            INSERT INTO Patients (
                Id, FirstName, LastName, Phone, Email, 
                Address, City, State, ZipCode, BirthDate, ReportReady
            )
            VALUES (
                @id, @firstName, @lastName, @phone, @email, 
                @address, @city, @state, @zipCode, @birthDate, @reportReady
            )
        """;
        
        var savedCount = await _context.ExecuteInTransactionAsync(async (transaction) =>
        {
            var count = 0;
            foreach (var patient in patients)
            {
                using var command = new SQLiteCommand(sql, _context.Connection, transaction);
                
                command.Parameters.AddWithValue("@id", patient.Id);
                command.Parameters.AddWithValue("@firstName", patient.FirstName);
                command.Parameters.AddWithValue("@lastName", patient.LastName);
                command.Parameters.AddWithValue("@phone", patient.Phone);
                command.Parameters.AddWithValue("@email", patient.Email ?? "");
                command.Parameters.AddWithValue("@address", patient.Address);
                command.Parameters.AddWithValue("@city", patient.City);
                command.Parameters.AddWithValue("@state", patient.State);
                command.Parameters.AddWithValue("@zipCode", patient.ZipCode);
                command.Parameters.AddWithValue("@birthDate", patient.DateOfBirth.ToString("yyyy-MM-dd"));
                command.Parameters.AddWithValue("@reportReady", patient.ReportReady);
                
                await command.ExecuteNonQueryAsync();
                count++;
                
                if (count % 100 == 0)
                {
                    _logger.LogInfo($"Progress: Saved {count}/{patients.Count} patients");
                }
            }
            return count;
        });
        
        return savedCount;
    }
}