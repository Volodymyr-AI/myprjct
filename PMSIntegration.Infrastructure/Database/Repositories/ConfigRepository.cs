using System.Data.SQLite;
using PMSIntegration.Application.Logging;

namespace PMSIntegration.Infrastructure.Database.Repositories;

public class ConfigRepository
{
    private readonly DatabaseContext _context;
    private readonly FileLogger _logger;
    
    public ConfigRepository(DatabaseContext context, FileLogger logger)
    {
        _context = context;
        _logger = logger;
    }
    
    /// <summary>
    /// Save or update config value
    /// </summary>
    public async Task SetAsync(string key, string value)
    {
        const string sql = """
                               INSERT INTO Config (Key, Value) 
                               VALUES (@key, @value)
                               ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value;
                           """;
        
        using var command = new SQLiteCommand(sql, _context.Connection);
        
        command.Parameters.AddWithValue("@key", key);
        command.Parameters.AddWithValue("@value", value);
        
        await command.ExecuteNonQueryAsync();
    }
    
    /// <summary>
    /// Get config value
    /// </summary>
    public async Task<string> GetAsync(string key, string defaultValue = null)
    {
        const string sql = "SELECT Value FROM Config WHERE Key = @key";
        
        using var command = new SQLiteCommand(sql, _context.Connection);
        command.Parameters.AddWithValue("@key", key);
        
        var result = await command.ExecuteScalarAsync();
        return result?.ToString() ?? defaultValue;
    }
}