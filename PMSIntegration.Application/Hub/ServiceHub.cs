namespace PMSIntegration.Application.Hub;

public static class ServiceHub
{
    private static readonly string Environment = GetEnvironmentVariable();
    public static string GetDatabasePath(string configDbPath)
    {
        return IsDevelopment()
            ? Path.Combine(Directory.GetCurrentDirectory(), configDbPath)
            : Path.Combine(AppContext.BaseDirectory, configDbPath);
    }

    public static string GetLogsPath()
    {
        return IsDevelopment()
            ? Path.Combine(Directory.GetCurrentDirectory(), "Logs")
            : Path.Combine(AppContext.BaseDirectory, "Logs");
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
    
    public static string GetReportsPath()
    {
        return IsDevelopment()
            ? Path.Combine(Directory.GetCurrentDirectory(), "Reports")
            : Path.Combine(AppContext.BaseDirectory, "Reports");
    }
}