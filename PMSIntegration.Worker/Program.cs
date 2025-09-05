using PMSIntegration.Application.Configuration;
using PMSIntegration.Application.Configuration.Interface;
using PMSIntegration.Application.Hub;
using PMSIntegration.Application.Logging;
using PMSIntegration.Application.PMSInterfaces;
using PMSIntegration.Application.Report;
using PMSIntegration.Core.Configuration.Abstract;
using PMSIntegration.Core.Configuration.Enum;
using PMSIntegration.Core.Configuration.Models;
using PMSIntegration.Infrastructure.Database.Config;
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

            // ============ Configuration ============
            
            // Get configuration from INI file
            var configService = new IniConfigurationService("Configuration.ini");
            var integrationConfig = IntegrationConfigFactory.Create(configService);
            
            // Register configuration in DI
            builder.Services.AddSingleton<IAppConfigurationService>(configService);
            builder.Services.AddSingleton(integrationConfig);

            // ============ BASE SERVICES ============
            
            // Logging
            builder.Services.AddSingleton<FileLogger>(_ => 
                new FileLogger(ServiceHub.GetLogsPath()));

            // ============ PMS SERVICES ============
            
            // Register PMS-specific services basing on configuration
            RegisterPmsServices(builder.Services, integrationConfig);
            
            // ============ APPLICATION SERVICES ============
            
            builder.Services.AddScoped<OpenDentalHub>();

            // ============ INFRASTRUCTURE SERVICES ============
            
            // Patient export service - using ServiceProvider to get actual PMS service
            builder.Services.AddScoped<PatientExportService>();
            
            // Service for deleting imported reports
            builder.Services.AddScoped<CleanupService>();
            
            // Service for report uploading
            builder.Services.AddScoped<ReportUploadService>();

            // Startup service for database initialization
            builder.Services.AddSingleton<IStartupService, StartupService>();

            // ============ WORKER ============
            
            // Background service
            builder.Services.AddHostedService<PatientWorker>();
            builder.Services.AddHostedService<ReportWorker>();

            // Windows Service configuration
            builder.Services.AddWindowsService(options =>
            {
                options.ServiceName = "PMSIntegration";
            });

            var host = builder.Build();
            var logger = host.Services.GetRequiredService<FileLogger>();
            logger.LogInfo("===========================================");
            logger.LogInfo($"PMSIntegration Service Starting");
            logger.LogInfo($"Provider: {integrationConfig.Provider}");
            logger.LogInfo($"Environment: {Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}");
            logger.LogInfo("===========================================");
            host.Run();
        }

        private static void RegisterPmsServices(IServiceCollection services, IntegrationConfig config)
        {
            switch (config.Provider)
            {
                case PmsProvider.OpenDental:
                    if (config is OpenDentalIntegrationConfig openDentalConfig)
                    {
                        services.AddScoped<OpenDentalService>(provider =>
                        {
                            var logger = provider.GetRequiredService<FileLogger>();
                            return new OpenDentalService(openDentalConfig, logger);
                        });
                    }
                    break;
                    
                case PmsProvider.Dentrix:
                    // TODO: Register Dentrix services when implemented
                    break;
                    
                case PmsProvider.EagleSoft:
                    // TODO: Register EagleSoft services when implemented
                    break;
                    
                default:
                    throw new NotSupportedException($"PMS Provider {config.Provider} is not supported");
            }
        }
    }
}