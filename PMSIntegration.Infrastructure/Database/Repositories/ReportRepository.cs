using System.Data.SQLite;
using PMSIntegration.Application.Logging;
using PMSIntegration.Core.Reports.Enums;

namespace PMSIntegration.Infrastructure.Database.Repositories;

public class ReportRepository
{
    private readonly DatabaseContext _context;
    private readonly FileLogger _logger;
    
    public ReportRepository(DatabaseContext context, FileLogger logger)
    {
        _context = context;
        _logger = logger;
    }
    
    /// <summary>
    /// Insert new uploaded report
    /// </summary>
    public async Task<int> InsertUploadedAsync(string fileName, string originalPath)
    {
        const string sql = """
            INSERT INTO Reports (FileName, OriginalPath, Status, CreatedAt)
            VALUES (@fileName, @originalPath, @status, @createdAt);
            SELECT last_insert_rowid();
        """;
        
        using var command = new SQLiteCommand(sql, _context.Connection);
        
        command.Parameters.AddWithValue("@fileName", fileName);
        command.Parameters.AddWithValue("@originalPath", originalPath);
        command.Parameters.AddWithValue("@status", ReportStatus.UPLOADED.ToString());
        command.Parameters.AddWithValue("@createdAt", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
        
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }
    
    /// <summary>
    /// Update report status
    /// </summary>
    public async Task UpdateStatusAsync(int reportId, ReportStatus status, string additionalInfo = null)
    {
        var sql = status switch
        {
            ReportStatus.PROCESSED => """
                UPDATE Reports 
                SET Status = @status, PatientName = @info, ProcessedAt = @timestamp
                WHERE Id = @id
            """,
            ReportStatus.IMPORTED => """
                UPDATE Reports 
                SET Status = @status, DestinationPath = @info, ImportedAt = @timestamp
                WHERE Id = @id
            """,
            ReportStatus.SUCCESS => """
                UPDATE Reports 
                SET Status = @status, CompletedAt = @timestamp
                WHERE Id = @id
            """,
            ReportStatus.FAILED => """
                UPDATE Reports 
                SET Status = @status, ErrorMessage = @info
                WHERE Id = @id
            """,
            _ => throw new ArgumentException($"Unknown status: {status}")
        };
        
        using var command = new SQLiteCommand(sql, _context.Connection);
        
        command.Parameters.AddWithValue("@id", reportId);
        command.Parameters.AddWithValue("@status", status.ToString());
        command.Parameters.AddWithValue("@info", additionalInfo ?? "");
        command.Parameters.AddWithValue("@timestamp", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
        
        await command.ExecuteNonQueryAsync();
    }
}