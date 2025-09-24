using PMSIntegration.Core.Configuration.Enum;

namespace PMSIntegration.Core.Configuration.Abstract;

public abstract class IntegrationConfig
{
    public PmsProvider Provider { get; set; }
    public DateTimeOffset ExportStartDate { get; set; }
}