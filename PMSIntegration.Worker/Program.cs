using PMSIntegration.Application.Configuration;
using PMSIntegration.Application.Configuration.Interface;
using PMSIntegration.Application.Hub;
using PMSIntegration.Application.Logging;
using PMSIntegration.Core.Configuration.Abstract;
using PMSIntegration.Core.Configuration.Enum;
using PMSIntegration.Core.Configuration.Models;
using PMSIntegration.Infrastructure.Configuration;
using PMSIntegration.Infrastructure.Database;
using PMSIntegration.Infrastructure.OpenDental.Services;
using PMSIntegration.Infrastructure.Services;
using PMSIntegration.Worker.Workers;

namespace PMSIntegration.Worker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            // Configuration
            var configService = new IniConfigurationService("Configuration.ini");
            var integrationConfig = IntegrationConfigFactory.Create(configService);
            
            builder.Services.AddSingleton<IAppConfigurationService>(configService);
            builder.Services.AddSingleton(integrationConfig);

            // Core Services
            builder.Services.AddSingleton<FileLogger>(_ => 
                new FileLogger(ServiceHub.GetLogsPath()));
            
            // Database - Scoped для кожного використання
            builder.Services.AddScoped<DatabaseInitializer>();
            builder.Services.AddScoped<DatabaseContext>();

            // PMS Services
            RegisterPmsServices(builder.Services, integrationConfig);
            
            // Application Services
            builder.Services.AddScoped<PatientExportService>();
            builder.Services.AddScoped<ReportUploadService>();

            // Workers
            builder.Services.AddHostedService<PatientWorker>();

            // Windows Service
            builder.Services.AddWindowsService(options =>
            {
                options.ServiceName = "PMSIntegration";
            });

            var host = builder.Build();
            
            // Initialize database and save configuration
            using (var scope = host.Services.CreateScope())
            {
                var dbInitializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
                var logger = scope.ServiceProvider.GetRequiredService<FileLogger>();
                
                logger.LogInfo("===========================================");
                logger.LogInfo($"PMSIntegration Service Starting");
                logger.LogInfo($"Provider: {integrationConfig.Provider}");
                
                // Initialize database
                dbInitializer.Initialize();
                
                // Save configuration to database
                SaveConfigurationToDatabase(scope.ServiceProvider, integrationConfig, configService);
                
                logger.LogInfo("Database initialized successfully");
                logger.LogInfo("===========================================");
            }
            
            host.Run();
        }

        private static void SaveConfigurationToDatabase(
            IServiceProvider serviceProvider, 
            IntegrationConfig config,
            IAppConfigurationService configService)
        {
            using var db = serviceProvider.GetRequiredService<DatabaseContext>();
            
            // Save main configuration
            db.Config.SetAsync("Provider", config.Provider.ToString()).Wait();
            db.Config.SetAsync("ExportStartDate", config.ExportStartDate.ToString("yyyy-MM-dd")).Wait();
            
            // Save OpenDental specific configuration
            if (config is OpenDentalIntegrationConfig openDentalConfig)
            {
                db.Config.SetAsync("AuthScheme", openDentalConfig.AuthScheme).Wait();
                db.Config.SetAsync("AuthToken", openDentalConfig.AuthToken).Wait();
                db.Config.SetAsync("ApiBaseUrl", openDentalConfig.ApiBaseUrl).Wait();
                db.Config.SetAsync("TimeoutSeconds", openDentalConfig.TimeoutSeconds.ToString()).Wait();
                db.Config.SetAsync("OpenDentalImagePath", openDentalConfig.OpenDentalImagePath).Wait();
            }
            
            // Save initialization info
            db.Config.SetAsync("LastInitialized", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")).Wait();
            db.Config.SetAsync("Version", "1.0.0").Wait();
        }

        private static void RegisterPmsServices(IServiceCollection services, IntegrationConfig config)
        {
            switch (config.Provider)
            {
                case PmsProvider.OpenDental:
                    if (config is OpenDentalIntegrationConfig openDentalConfig)
                    {
                        services.AddScoped(provider =>
                        {
                            var logger = provider.GetRequiredService<FileLogger>();
                            return new OpenDentalService(openDentalConfig, logger);
                        });
                    }
                    break;
                    
                case PmsProvider.Dentrix:
                case PmsProvider.EagleSoft:
                    // TODO: Implement when needed
                    break;
                    
                default:
                    throw new NotSupportedException($"PMS Provider {config} is not supported");
            }
        }
    }
}