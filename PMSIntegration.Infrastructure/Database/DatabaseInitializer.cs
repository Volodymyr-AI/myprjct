using System.Data.SQLite;
using PMSIntegration.Infrastructure.Database.Config;
using PMSIntegration.Infrastructure.Database.Insurance;
using PMSIntegration.Infrastructure.Database.Patient;
using PMSIntegration.Infrastructure.Database.Report;

namespace PMSIntegration.Infrastructure.Database;

public static class DatabaseInitializer
{
    public static void Initialize(string dbPath)
    {
        var fileExists = File.Exists(dbPath);

        if (!fileExists)
        {
            SQLiteConnection.CreateFile(dbPath);
        }
        
        using var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;");
        connection.Open();
        
        CreateTables(connection);
    }

    private static void CreateTables(SQLiteConnection connection)
    {
        PatientTable.CreatePatientTable(connection);
        ConfigTable.CreateConfigTable(connection);
        ReportTable.CreateReportTable(connection);
        InsuranceTable.CreateInsuranceTable(connection);
    }

    public static void ShutDownConnection(SQLiteConnection connection)
    {
        if(connection.State != System.Data.ConnectionState.Open)
            return;
        
        connection.Close();
    }
}