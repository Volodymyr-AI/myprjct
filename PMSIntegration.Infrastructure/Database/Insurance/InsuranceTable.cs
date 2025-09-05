using System.Data.SQLite;
using PMSIntegration.Core.Patients.Models;

namespace PMSIntegration.Infrastructure.Database.Insurance;

public static class InsuranceTable
{
    public static void CreateInsuranceTable(SQLiteConnection connection)
    {
        string createInsuranceTableSql = """
                                             CREATE TABLE IF NOT EXISTS Insurance (
                                                 Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                                 PatientId INTEGER NOT NULL,
                                                 InsSubNum INTEGER NOT NULL,
                                                 PatPlanNum INTEGER NOT NULL,
                                                 CarrierName TEXT NOT NULL,
                                                 SubscriberId TEXT NOT NULL,
                                                 SubscriberName TEXT NOT NULL,
                                                 Relationship TEXT NOT NULL,
                                                 GroupName TEXT NOT NULL,
                                                 GroupNumber TEXT NOT NULL,
                                                 PlanType TEXT NOT NULL,
                                                 Ordinal TEXT NOT NULL,
                                                 IsPending BOOLEAN NOT NULL,
                                                 IsMedical BOOLEAN NOT NULL,
                                                 EmployerName TEXT NOT NULL,
                                                 PlanNote TEXT NOT NULL,
                                                 CreatedAt TEXT NOT NULL,
                                                 UpdatedAt TEXT NOT NULL,
                                                 FOREIGN KEY (PatientId) REFERENCES Patients (Id),
                                                 UNIQUE(PatientId, PatPlanNum)
                                             );
                                         """;
        
        using var command = new SQLiteCommand(createInsuranceTableSql, connection);
        command.ExecuteNonQuery();
    }
    
    public static async Task<int> SaveInsuranceAsync(
        SQLiteConnection connection, 
        PatientInsurance insurance)
    {
        const string insertSql = """
            INSERT OR REPLACE INTO Insurance (
                PatientId, InsSubNum, PatPlanNum, CarrierName, SubscriberId,
                SubscriberName, Relationship, GroupName, GroupNumber, PlanType,
                Ordinal, IsPending, IsMedical, EmployerName, PlanNote,
                CreatedAt, UpdatedAt
            ) VALUES (
                @patientId, @insSubNum, @patPlanNum, @carrierName, @subscriberId,
                @subscriberName, @relationship, @groupName, @groupNumber, @planType,
                @ordinal, @isPending, @isMedical, @employerName, @planNote,
                @createdAt, @updatedAt
            );
            SELECT last_insert_rowid();
        """;
        
        using var command = new SQLiteCommand(insertSql, connection);
        
        command.Parameters.AddWithValue("@patientId", insurance.PatientId.Value);
        command.Parameters.AddWithValue("@insSubNum", insurance.InsSubNum);
        command.Parameters.AddWithValue("@patPlanNum", insurance.PatPlanNum);
        command.Parameters.AddWithValue("@carrierName", insurance.CarrierName);
        command.Parameters.AddWithValue("@subscriberId", insurance.SubscriberId);
        command.Parameters.AddWithValue("@subscriberName", insurance.SubscriberName);
        command.Parameters.AddWithValue("@relationship", insurance.Relationship);
        command.Parameters.AddWithValue("@groupName", insurance.GroupName);
        command.Parameters.AddWithValue("@groupNumber", insurance.GroupNumber);
        command.Parameters.AddWithValue("@planType", insurance.PlanType);
        command.Parameters.AddWithValue("@ordinal", insurance.Ordinal);
        command.Parameters.AddWithValue("@isPending", insurance.IsPending ? 1 : 0);
        command.Parameters.AddWithValue("@isMedical", insurance.IsMedical ? 1 : 0);
        command.Parameters.AddWithValue("@employerName", insurance.EmployerName);
        command.Parameters.AddWithValue("@planNote", insurance.PlanNote);
        command.Parameters.AddWithValue("@createdAt", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
        
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }
}