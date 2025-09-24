using System.Data.SQLite;
using PMSIntegration.Application.Hub;
using PMSIntegration.Application.Logging;

namespace PMSIntegration.Infrastructure.Database;

public class DatabaseInitializer
{
    private readonly FileLogger _logger;
    private readonly string _dbPath;
    
    public DatabaseInitializer(FileLogger logger)
    {
        _logger = logger;
        _dbPath = ServiceHub.GetDatabasePath();
    }
    
    /// <summary>
    /// Initialize database and create all tables
    /// </summary>
    public void Initialize()
    {
        try
        {
            var fileExists = File.Exists(_dbPath);
            
            if (!fileExists)
            {
                SQLiteConnection.CreateFile(_dbPath);
                _logger.LogInfo($"Created new database at: {_dbPath}");
            }
            
            using var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;");
            connection.Open();
            
            CreateTables(connection);
            
            _logger.LogInfo("Database initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize database");
            throw;
        }
    }
    
    private void CreateTables(SQLiteConnection connection)
    {
        // Create Patients table
        ExecuteNonQuery(connection, """
            CREATE TABLE IF NOT EXISTS Patients (
                Id INTEGER PRIMARY KEY,
                FirstName TEXT NOT NULL,
                LastName TEXT NOT NULL,
                Phone TEXT NOT NULL,
                Email TEXT,
                Address TEXT NOT NULL,
                City TEXT NOT NULL,
                State TEXT NOT NULL,
                ZipCode TEXT NOT NULL,
                BirthDate TEXT NOT NULL,
                ReportReady BOOLEAN NOT NULL DEFAULT 0
            );
        """);
        
        // Create Insurance table
        ExecuteNonQuery(connection, """
            CREATE TABLE IF NOT EXISTS Insurance (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                PatientId INTEGER NOT NULL,
                CarrierName TEXT NOT NULL,
                PolicyNumber TEXT NOT NULL,
                GroupNumber TEXT,
                PolicyholderName TEXT NOT NULL,
                Relationship TEXT NOT NULL,
                Priority TEXT NOT NULL,
                IsActive BOOLEAN NOT NULL DEFAULT 1,
                CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (PatientId) REFERENCES Patients (Id),
                UNIQUE(PatientId, CarrierName, PolicyNumber)
            );
        """);
        
        // Create Reports table
        ExecuteNonQuery(connection, """
            CREATE TABLE IF NOT EXISTS Reports (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                FileName TEXT NOT NULL,
                OriginalPath TEXT NOT NULL,
                PatientName TEXT,
                DestinationPath TEXT,
                Status TEXT NOT NULL,
                ErrorMessage TEXT,
                CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                ProcessedAt TEXT,
                ImportedAt TEXT,
                CompletedAt TEXT
            );
        """);
        
        // Create Config table
        ExecuteNonQuery(connection, """
            CREATE TABLE IF NOT EXISTS Config (
                Key TEXT PRIMARY KEY,
                Value TEXT NOT NULL
            );
        """);
        
        _logger.LogInfo("All database tables created/verified");
    }
    
    private void ExecuteNonQuery(SQLiteConnection connection, string sql)
    {
        using var command = new SQLiteCommand(sql, connection);
        command.ExecuteNonQuery();
    }
}