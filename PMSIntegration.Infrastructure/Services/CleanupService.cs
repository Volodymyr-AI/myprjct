using System.Data.SQLite;
using PMSIntegration.Application.Hub;
using PMSIntegration.Application.Logging;
using PMSIntegration.Core.Configuration.Abstract;
using PMSIntegration.Core.Reports.Enums;

namespace PMSIntegration.Infrastructure.Services;

public class CleanupService
{
    private readonly FileLogger _logger;

    public CleanupService(FileLogger logger)
    {
        _logger = logger;
    }
    public async Task Cleanup(string databasePath)
    {
        try
        {
            using var connection = new SQLiteConnection($"Data Source={databasePath};Version=3;");
            await connection.OpenAsync();

            const string selectSql = """
                                         SELECT OriginalPath 
                                         FROM Reports 
                                         WHERE Status = @status
                                     """;

            using var command = new SQLiteCommand(selectSql, connection);
            command.Parameters.AddWithValue("@status", ReportStatus.SUCCESS);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var filePath = reader.GetString(0);

                if (File.Exists(filePath))
                {
                    try
                    {
                        File.Delete(filePath);
                        _logger.LogInfo($"Report file: {filePath} deleted");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to delete successful report: {filePath}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cleanup of successful reports");
        }
    }
}