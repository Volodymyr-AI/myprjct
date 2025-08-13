using PMSIntegration.Application.Configuration;
using PMSIntegration.Application.Configuration.Interface;
using PMSIntegration.Application.Interfaces;
using PMSIntegration.Application.Logging;
using PMSIntegration.Core.Configuration.Abstract;
using PMSIntegration.Core.Configuration.Enum;
using PMSIntegration.Core.Configuration.Models;
using PMSIntegration.Infrastructure.Database;
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

            var configService = new IniConfigurationService("Configuration.ini");
            var integrationConfig = IntegrationConfigFactory.Create(configService);

            builder.Services.AddSingleton<IAppConfigurationService>(configService);
            builder.Services.AddSingleton(integrationConfig);

            builder.Services.AddSingleton<FileLogger>(_ => new FileLogger("Logs"));
            
            RegisterPmsServices(builder.Services, integrationConfig);

            builder.Services.AddScoped<PatientExportService>();
            builder.Services.AddSingleton<IStartupService, StartupService>();

            builder.Services.AddHostedService<Worker>();

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