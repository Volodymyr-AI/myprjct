using System.Data.SQLite;
using PMSIntegration.Application.Hub;
using PMSIntegration.Application.Logging;
using PMSIntegration.Infrastructure.Database.Repositories;

namespace PMSIntegration.Infrastructure.Database;

/// <summary>
/// Main database context - manages connection and provides repositories
/// </summary>
public class DatabaseContext : IDisposable
{
    private readonly string _connectionString;
    private SQLiteConnection _connection;
    private readonly FileLogger _logger;
    
    // Lazy-loaded repositories
    private PatientRepository _patients;
    private InsuranceRepository _insurance;
    private ReportRepository _reports;
    private ConfigRepository _config;
    
    public DatabaseContext(FileLogger logger)
    {
        _logger = logger;
        var dbPath = ServiceHub.GetDatabasePath();
        _connectionString = $"Data Source={dbPath};Version=3;";
    }
    
    /// <summary>
    /// Get or create connection
    /// </summary>
    public SQLiteConnection Connection
    {
        get
        {
            if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
            {
                _connection = new SQLiteConnection(_connectionString);
                _connection.Open();
            }
            return _connection;
        }
    }
    
    /// <summary>
    /// Repository for Patient operations
    /// </summary>
    public PatientRepository Patients => 
        _patients ??= new PatientRepository(this, _logger);
    
    /// <summary>
    /// Repository for Insurance operations
    /// </summary>
    public InsuranceRepository Insurance => 
        _insurance ??= new InsuranceRepository(this, _logger);
    
    /// <summary>
    /// Repository for Report operations
    /// </summary>
    public ReportRepository Reports => 
        _reports ??= new ReportRepository(this, _logger);
    
    /// <summary>
    /// Repository for Config operations
    /// </summary>
    public ConfigRepository Config => 
        _config ??= new ConfigRepository(this, _logger);
    
    /// <summary>
    /// Execute in transaction
    /// </summary>
    public async Task<T> ExecuteInTransactionAsync<T>(Func<SQLiteTransaction, Task<T>> action)
    {
        using var transaction = Connection.BeginTransaction();
        try
        {
            var result = await action(transaction);
            await transaction.CommitAsync();
            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    
    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
    }
}