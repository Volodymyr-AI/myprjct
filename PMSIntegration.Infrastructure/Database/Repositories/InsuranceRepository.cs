using System.Data.SQLite;
using PMSIntegration.Application.Logging;
using PMSIntegration.Core.Patients.Models;

namespace PMSIntegration.Infrastructure.Database.Repositories;

public class InsuranceRepository
{
    private readonly DatabaseContext _context;
    private readonly FileLogger _logger;
    
    public InsuranceRepository(DatabaseContext context, FileLogger logger)
    {
        _context = context;
        _logger = logger;
    }
    
    /// <summary>
    /// Save insurance record
    /// </summary>
    public async Task<int> SaveAsync(Insurance insurance)
    {
        const string sql = """
            INSERT OR REPLACE INTO Insurance (
                PatientId, CarrierName, PolicyNumber, GroupNumber,
                PolicyholderName, Relationship, Priority, IsActive
            ) VALUES (
                @patientId, @carrierName, @policyNumber, @groupNumber,
                @policyholderName, @relationship, @priority, @isActive
            );
            SELECT last_insert_rowid();
        """;
        
        using var command = new SQLiteCommand(sql, _context.Connection);
        
        command.Parameters.AddWithValue("@patientId", insurance.PatientId);
        command.Parameters.AddWithValue("@carrierName", insurance.CarrierName);
        command.Parameters.AddWithValue("@policyNumber", insurance.PolicyNumber);
        command.Parameters.AddWithValue("@groupNumber", insurance.GroupNumber ?? "");
        command.Parameters.AddWithValue("@policyholderName", insurance.PolicyholderName);
        command.Parameters.AddWithValue("@relationship", insurance.Relationship);
        command.Parameters.AddWithValue("@priority", insurance.Priority);
        command.Parameters.AddWithValue("@isActive", insurance.IsActive);
        
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }
    
    /// <summary>
    /// Save multiple insurance records
    /// </summary>
    public async Task<int> BulkSaveAsync(List<Insurance> insuranceList)
    {
        var savedCount = 0;
        
        await _context.ExecuteInTransactionAsync(async (transaction) =>
        {
            foreach (var insurance in insuranceList)
            {
                await SaveAsync(insurance);
                savedCount++;
            }
            return savedCount;
        });
        
        _logger.LogInfo($"Saved {savedCount} insurance records");
        return savedCount;
    }
}