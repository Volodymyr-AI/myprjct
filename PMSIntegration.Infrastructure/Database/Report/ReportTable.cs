using System.Data;
using System.Data.SQLite;
using PMSIntegration.Core.Reports.Enums;

namespace PMSIntegration.Infrastructure.Database.Report;

public static class ReportTable
{
    public static void CreateReportTable(SQLiteConnection connection)
    {
        string createReportTableSql = """
                                          CREATE TABLE IF NOT EXISTS Reports (
                                              Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                              FileName TEXT NOT NULL,
                                              OriginalPath TEXT NOT NULL,
                                              PatientName TEXT,
                                              DestinationPath TEXT,
                                              Status TEXT NOT NULL,
                                              ErrorMessage TEXT,
                                              CreatedAt TEXT NOT NULL,
                                              ProcessedAt TEXT,
                                              ImportedAt TEXT,
                                              CompletedAt TEXT
                                          );
                                      """;
        using var command = new SQLiteCommand(createReportTableSql, connection);
        command.ExecuteNonQuery();
    }

    public static async Task<int> InsertUploadedReport(
            SQLiteConnection connection,
            string fileName,
            string originalPath
        )
    {
        const string insertSql = """
                                     INSERT INTO Reports (
                                         FileName, OriginalPath, Status, CreatedAt
                                     ) VALUES (
                                         @fileName, @originalPath, @status, @createdAt
                                     );
                                     SELECT last_insert_rowid();
                                 """;
        using var command = new SQLiteCommand(insertSql, connection);
        command.Parameters.AddWithValue("@fileName", fileName);
        command.Parameters.AddWithValue("@originalPath", originalPath);
        command.Parameters.AddWithValue("@status", ReportStatus.UPLOADED.ToString());
        command.Parameters.AddWithValue("@createdAt", DateTime.UtcNow.ToString("yyyy-MM-dd"));
        
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    public static async Task UpdateToProcessed(
            SQLiteConnection connection,
            int reportId,
            string patientName
        )
    {
        const string updateSql = """
                                     UPDATE Reports 
                                     SET Status = @status,
                                         PatientName = @patientName,
                                         ProcessedAt = @processedAt
                                     WHERE Id = @id
                                 """;
        
        using var command = new SQLiteCommand(updateSql, connection);
        command.Parameters.AddWithValue("@id", reportId);
        command.Parameters.AddWithValue("@status", ReportStatus.PROCESSED.ToString());
        command.Parameters.AddWithValue("@patientName", patientName);
        command.Parameters.AddWithValue("@processedAt", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
        
        await command.ExecuteNonQueryAsync();
    }
    
    public static async Task UpdateToImported(
        SQLiteConnection connection,
        int reportId,
        string destinationPath)
    {
        const string updateSql = """
                                     UPDATE Reports 
                                     SET Status = @status,
                                         DestinationPath = @destinationPath,
                                         ImportedAt = @importedAt
                                     WHERE Id = @id
                                 """;
        
        using var command = new SQLiteCommand(updateSql, connection);
        command.Parameters.AddWithValue("@id", reportId);
        command.Parameters.AddWithValue("@status", ReportStatus.IMPORTED.ToString());
        command.Parameters.AddWithValue("@destinationPath", destinationPath);
        command.Parameters.AddWithValue("@importedAt", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
        
        await command.ExecuteNonQueryAsync();
    }
    
    public static async Task UpdateToSuccess(
        SQLiteConnection connection,
        int reportId)
    {
        const string updateSql = """
                                     UPDATE Reports 
                                     SET Status = @status,
                                         CompletedAt = @completedAt
                                     WHERE Id = @id
                                 """;
        
        using var command = new SQLiteCommand(updateSql, connection);
        command.Parameters.AddWithValue("@id", reportId);
        command.Parameters.AddWithValue("@status", ReportStatus.SUCCESS.ToString());
        command.Parameters.AddWithValue("@completedAt", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
        
        await command.ExecuteNonQueryAsync();
    }
    
    public static async Task UpdateToFailed(
        SQLiteConnection connection,
        int reportId,
        string errorMessage)
    {
        const string updateSql = """
                                     UPDATE Reports 
                                     SET Status = @status,
                                         ErrorMessage = @errorMessage
                                     WHERE Id = @id
                                 """;
        
        using var command = new SQLiteCommand(updateSql, connection);
        command.Parameters.AddWithValue("@id", reportId);
        command.Parameters.AddWithValue("@status", ReportStatus.FAILED.ToString());
        command.Parameters.AddWithValue("@errorMessage", errorMessage);
        
        await command.ExecuteNonQueryAsync();
    }
    
    public static async Task<List<(int Id, string FileName, string OriginalPath)>> GetUploadedReports(
        SQLiteConnection connection)
    {
        const string selectSql = """
                                     SELECT Id, FileName, OriginalPath 
                                     FROM Reports 
                                     WHERE Status = @status
                                     ORDER BY CreatedAt
                                 """;
        
        var reports = new List<(int, string, string)>();
        
        using var command = new SQLiteCommand(selectSql, connection);
        command.Parameters.AddWithValue("@status", ReportStatus.UPLOADED.ToString());
        
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            reports.Add((
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2)
            ));
        }
        
        return reports;
    }
}