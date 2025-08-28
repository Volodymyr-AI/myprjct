namespace PMSIntegration.Application.Hub;

public static class ServiceHub
{
    private const string DATABASE_NAME = "database.db";
    private const string LOGS_FOLDER = "Logs";
    private const string REPORTS_FOLDER = "Reports";

    private static readonly string Environment = GetEnvironmentVariable();
    public static string GetDatabasePath()
    {
        return IsDevelopment()
            ? Path.Combine(Directory.GetCurrentDirectory(), DATABASE_NAME)
            : Path.Combine(AppContext.BaseDirectory, DATABASE_NAME);
    }

    public static string GetLogsPath()
    {
        var path = IsDevelopment()
            ? Path.Combine(Directory.GetCurrentDirectory(), LOGS_FOLDER)
            : Path.Combine(AppContext.BaseDirectory, LOGS_FOLDER);
        
        Directory.CreateDirectory(path);
        return path;
    }
    
    public static string GetReportsPath()
    {
        var path = IsDevelopment()
            ? Path.Combine(Directory.GetCurrentDirectory(), REPORTS_FOLDER)
            : Path.Combine(AppContext.BaseDirectory, REPORTS_FOLDER);
        
        Directory.CreateDirectory(path);
        return path;
    }

    public static bool IsDevelopment()
    {
        return Environment == "Development";
    }

    public static bool IsProduction()
    {
        return !IsDevelopment();
    }

    private static string GetEnvironmentVariable()
    {
        return System.Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
    }
    
    public static string GetBasePath()
    {
        return IsDevelopment() 
            ? Directory.GetCurrentDirectory() 
            : AppContext.BaseDirectory;
    }
}