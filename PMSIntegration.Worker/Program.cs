using PMSIntegration.Application.Configuration;
using PMSIntegration.Application.Configuration.Interface;
using PMSIntegration.Application.Hub;
using PMSIntegration.Application.Interfaces;
using PMSIntegration.Application.Logging;
using PMSIntegration.Core.Configuration.Abstract;
using PMSIntegration.Core.Configuration.Enum;
using PMSIntegration.Core.Configuration.Models;
using PMSIntegration.Infrastructure.Database.Config;
using PMSIntegration.Infrastructure.OpenDental.Services;
using PMSIntegration.Infrastructure.Services;

namespace PMSIntegration.Worker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            // Configuration services
            var configService = new IniConfigurationService("Configuration.ini");
            var integrationConfig = IntegrationConfigFactory.Create(configService);
            
            var databasePath = ServiceHub.GetDatabasePath(integrationConfig.DbPath);
            var logsPath = ServiceHub.GetLogsPath();

            builder.Services.AddSingleton<IAppConfigurationService>(configService);
            builder.Services.AddSingleton(integrationConfig);
            builder.Services.AddSingleton(_ => databasePath);

            // Infrastructure services
            builder.Services.AddSingleton<FileLogger>(_ => new FileLogger(logsPath));
            
            // Register PMS-specific services
            RegisterPmsServices(builder.Services, integrationConfig);

            // Application services
            builder.Services.AddScoped<PatientExportService>(provider =>
            {
                var config = provider.GetRequiredService<IntegrationConfig>();
                var logger = provider.GetRequiredService<FileLogger>();
                var dbPath = provider.GetRequiredService<string>();
                var openDentalService = provider.GetService<OpenDentalService>();
                
                return new PatientExportService(config, logger, dbPath, openDentalService);
            });

            builder.Services.AddSingleton<IStartupService>(provider =>
            {
                var config = provider.GetRequiredService<IntegrationConfig>();
                var dbPath = provider.GetRequiredService<string>();
                
                return new StartupService(config, dbPath);
            });

            // Worker service
            builder.Services.AddHostedService<Worker>();

            // Windows service configuration
            builder.Services.AddWindowsService(options =>
            {
                options.ServiceName = "PMSIntegration";
            });

            var host = builder.Build();
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